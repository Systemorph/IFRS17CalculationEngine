using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenSmc.Activities;
using OpenSmc.DataSource.Abstractions;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Messaging;
using OpenSmc.Reflection;
using OpenSmc.ServiceProvider;
using System.Collections.Immutable;
using IWorkspace = OpenSmc.Workspace.IWorkspace;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

/* AM (1.2.2024) todo list:
 *  a) organize better the IFRS17 project structure
 *  b) orchestrate which data go to which hub, e.g. refDataHub owns all dimensions
 *      write the configuration for parameters, transactionalData, etc etc
 *  c) finish setting up all model hubs simply by means of this generic DataPlugin
 *  d) implement tests for the DataPlugin by adding tests in the OpenSMC repo
 *  e) test the IFRS17 model hubs writing the level above this configurations
 *      to do this check the existing tests in the OpenSMC, e.g. MessageHubTest
 *      then write the configuration to define routing, addresses, forwarding, etc $
 *  f) think at the viewModelHub, what to do here? where to start?
 *      look at the existing tests in OpenSMC, e.g. LayoutTest
 *  g) monitor the development of the import/export plugin so that we can use them here
 *      in smc v1 
 */
public static class IfrsConfiguration
{
    public static MessageHubConfiguration ConfigurationReferenceDataHub(this MessageHubConfiguration configuration)
    {
        // TODO: this needs to be registered in the higher level
        var dataSource = configuration.ServiceProvider.GetService<IDataSource>();

        return configuration
            .AddData(data => data
                .WithType<Scenario>( // TODO: Extend With Type to avoid monkey code
                    async () => await dataSource.Query<Scenario>().ToArrayAsync(),
                    (scenarios) => dataSource.UpdateAsync(scenarios),
                    (scenarios) => dataSource.DeleteAsync(scenarios))
                .WithType<AmountType>(
                    async () => await dataSource.Query<AmountType>().ToArrayAsync(),
                    (amountTypes) => dataSource.UpdateAsync(amountTypes),
                    (amountTypes) => dataSource.DeleteAsync(amountTypes)));
    }

    public static MessageHubConfiguration ConfigurationTransactionalDataHub(this MessageHubConfiguration configuration)
    {
        // TODO: this needs to be registered in the higher level
        var dataSource = configuration.ServiceProvider.GetService<IDataSource>();

        return configuration
            .AddData(data => data.WithType<RawVariable>(
                async () => await dataSource.Query<RawVariable>().ToArrayAsync(),
                (scenarios) => dataSource.UpdateAsync(scenarios),
                (scenarios) => dataSource.DeleteAsync(scenarios))); /* This is delete of data, not of the hub */
        /* Delete of Hub must be implemented separately (pr)*/
    }

    public static MessageHubConfiguration ConfigurationViewModelHub(this MessageHubConfiguration configuration)
    {
        // TODO: this needs to be registered in the higher level
        //var dataSource = configuration.ServiceProvider.GetService<IDataSource>();
        // TODO: What content should be here? -A.K.

        /* AM (1.2.2024) personal idea:
         *  The view model (VM) hub is attached to the browser tab, namely there is one per tab.
         *  When one control (e.g. import button) hosted in this VM signals that an import has to start,
         *  an import request should be sent to the ImportPlugin. Hence the ImportPlugin has to be 
         *  'attached' to this VM. 
         *  
         *  This also grants scalability. Because ImportPlugin knows the import formats and calls 
         *  the method to perform the calculation, this setup ensures one calculation can take place
         *  per browser tab.
         */

        return configuration;
    }
}

/* TODO List: 
 *  a) move code DataPlugin to opensmc
 *  b) create an immutable variant of the workspace
 *  c) make workspace methods fully sync
 *  d) offload saves & deletes to a different hub
 *  e) configure Ifrs Hubs
 */
public static class DataPluginExtensions
{
    public static MessageHubConfiguration AddData(this MessageHubConfiguration configuration, 
        Func<DataPluginConfiguration, DataPluginConfiguration> dataConfiguration) 
        => configuration.AddPlugin(hub => new DataPlugin(hub, configuration, dataConfiguration));
}

public record DataPluginConfiguration
{
    internal ImmutableList<TypeConfiguration> TypeConfigurations { get; private set; }

    public DataPluginConfiguration WithType<T>(
        Func<Task<IReadOnlyCollection<T>>> initialize,
        Func<IReadOnlyCollection<T>, Task> save,
        Func<IReadOnlyCollection<object>, Task> delete) 
        => this with { TypeConfigurations = TypeConfigurations.Add(new TypeConfiguration<T>(initialize, save, delete)) };
}

public record TypeConfiguration<T> (
    Func<Task<IReadOnlyCollection<T>>> Initialize,
    Func<IReadOnlyCollection<T>, Task> Save,
    Func<IReadOnlyCollection<object>, Task> Delete) : TypeConfiguration;

public abstract record TypeConfiguration();

public record TypeConfigurationWithType<T>(TypeConfiguration<T> TypeConfiguration, Type Type);




public class DataPlugin : MessageHubPlugin<DataPlugin, IWorkspace>
{
    [Inject] private IActivityService activityService;

    private IDataSource dataSource;

    private DataPluginConfiguration DataConfiguration { get; set; } = new();

    public DataPlugin(IMessageHub hub, MessageHubConfiguration configuration, 
                      Func<DataPluginConfiguration, DataPluginConfiguration> dataConfiguration) : base(hub)
    {
        Register(HandleGetRequest);              // This takes care of all Read (CRUD)
        Register(HandleUpdateAndDeleteRequest);  // This takes care of all Update and Delete (CRUD)

        DataConfiguration = dataConfiguration(DataConfiguration);
    }

    public override async Task StartAsync()  // This takes care of the Create (CRUD)
    {
        await base.StartAsync();

        foreach(var typeConfig in DataConfiguration.TypeConfigurations)
        {
            var config = (TypeConfiguration<object>)typeConfig;
            var items = await config.Initialize();
            await State.UpdateAsync(items);
        }
    }

    private IMessageDelivery HandleUpdateAndDeleteRequest(IMessageDelivery request)
    {
        var type = request.Message.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(UpdateBatchRequest<>) || type.GetGenericTypeDefinition() == typeof(DeleteBatchRequest<>))
        {
            var elementType = type.GetGenericArguments().First();
            var typeConfig = DataConfiguration.TypeConfigurations.FirstOrDefault(x => 
                    x.GetType().GetGenericArguments().First() == elementType );  // TODO: check whether this works

            if (typeConfig is null) return request;
            var config = (TypeConfiguration<object>)typeConfig;

            if(type.GetGenericTypeDefinition() == typeof(UpdateBatchRequest<>))
            {
                var getElementsMethod = ReflectionHelper.GetMethodGeneric<DataPlugin>(x => x.UpdateElements<object>(null, null));
                getElementsMethod.MakeGenericMethod(elementType).InvokeAsFunction(this, config.Save);
            }
            else if(type.GetGenericTypeDefinition() == typeof(DeleteBatchRequest<>))
            {
                var getElementsMethod = ReflectionHelper.GetMethodGeneric<DataPlugin>(x => x.DeleteElements<object>(null, null));
                getElementsMethod.MakeGenericMethod(elementType).InvokeAsFunction(this, config.Delete);
            }
        }
        return request.Processed();
    }

    async Task UpdateElements<T>(IMessageDelivery<UpdateBatchRequest<T>> request, Func<IReadOnlyCollection<T>, Task> save) where T : class
    {
        var items = request.Message.Elements;
        await save(items);                     // save to db
        await State.UpdateAsync(items);        // update the state in memory (workspace)
        Hub.Post(new DataChanged(items));      // notify all subscribers that the data has changed
    }

    async Task DeleteElements<T>(IMessageDelivery<DeleteBatchRequest<T>> request, Func<IReadOnlyCollection<T>, Task> delete) where T : class
    {
        var items = request.Message.Elements;
        await delete(items);
        await State.DeleteAsync(items);
        Hub.Post(new DataDeleted(items));
    }

    private IMessageDelivery HandleGetRequest(IMessageDelivery request)
    {
        var type = request.Message.GetType();
        if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(GetManyRequest<>))
        {
            var elementType = type.GetGenericArguments().First();
            var getElementsMethod = ReflectionHelper.GetMethodGeneric<DataPlugin>(x => x.GetElements<object>(null));
            return (IMessageDelivery)getElementsMethod.MakeGenericMethod(elementType).InvokeAsFunction(this, request);
        }
        return request;
    }

    private IMessageDelivery GetElements<T>(IMessageDelivery<GetManyRequest<T>> request) where T : class
    {
        var query = State.Query<T>();
        var message = request.Message;
        if(message.PageSize is not null)
            query = query.Skip(message.Page * message.PageSize.Value).Take(message.PageSize.Value);
        var queryResult = query.ToArray();
        Hub.Post(queryResult, o => o.ResponseFor(request));
        return request.Processed();
    }
}


/* Create request:
 *      RegisterCallback --> send ImportRequest to ImportHub
 *      
 *          ImportHub: processes this request and respond with data
 *  
 *      upon receiving the response, this hub has the data and can initialize the state
 *  
 * Read requests:
 *      send a response with the data needed
 *      
 * Update requests:
 *      perform validation
 *      
 *      if validation fails, respond with the failing log
 *      
 *      if validation succeeds, respond with DataChangedEvent 
 *      
 *      DataChangedEvent will be received to all subscribers, e.g.
 *       - ui controls that need to update with new data
 *       - DataHub receives this and updates the ref data in its cache
 */

