using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenSmc.Activities;
using OpenSmc.DataSource.Abstractions;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Messaging;
using OpenSmc.Reflection;
using OpenSmc.ServiceProvider;
using OpenSmc.Workspace;
using System.Collections.Immutable;
using System.Reflection;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

/* For now, this hub owns dimensions solely (subject to change).
 * This hub performs CRUD operations, e.g.
 *  - create: either instantiate the state from DB or perform an in-memory init with import
 *  - read: respond with one or more collection of dimension instances
 *  - update: after a successful import, this hub receives an update request with the new data
 *  
 *  This hub needs to be attached to the ImportPlugin because in case of a in-memory init,
 *  template dimensions have to be written from files.
 */

public static class IfrsConfiguration
{
    public static MessageHubConfiguration ConfigurationReferenceDataHub(this MessageHubConfiguration configuration)
    {
        // TODO: this needs to be registered in the higher level
        var dataSource = configuration.ServiceProvider.GetService<IDataSource>();

        return configuration
            .AddData(data => data.WithType<Scenario>(
                async () => await dataSource.Query<Scenario>().ToArrayAsync(),
                (scenarios) => dataSource.UpdateAsync(scenarios),
                (scenarios) => dataSource.DeleteAsync(scenarios)));
    }

    public static MessageHubConfiguration ConfigurationTransactionalDataHub(this MessageHubConfiguration configuration)
    {
        // TODO: this needs to be registered in the higher level
        var dataSource = configuration.ServiceProvider.GetService<IDataSource>();

        return configuration
            .AddData(data => data.WithType<RawVariable>(
                async () => await dataSource.Query<RawVariable>().ToArrayAsync(),
                (scenarios) => dataSource.UpdateAsync(scenarios),
                (scenarios) => dataSource.DeleteAsync(scenarios)));
    }

    public static MessageHubConfiguration ConfigurationViewModelHub(this MessageHubConfiguration configuration)
    {
        // TODO: this needs to be registered in the higher level
        var dataSource = configuration.ServiceProvider.GetService<IDataSource>();

        return configuration
            .;
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
        => configuration;
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

public record TypeConfiguration (); 

public class DataPlugin : MessageHubPlugin<DataPlugin, IWorkspace>,
                          IMessageHandler<GetManyRequest<Scenario>>
{
    [Inject] private IActivityService activityService;

    private IDataSource dataSource;

    public DataPlugin(IMessageHub hub, MessageHubConfiguration options) : base(hub)
    {
        Register(HandleGetRequest);
    }

    public override async Task StartAsync()
    {
        await base.StartAsync();

        // TODO: consider making this a configuration rather than having it here
        var items = await dataSource.Query<Scenario>().ToArrayAsync();
        await State.UpdateAsync(items);

        // TODO: UpdateState needs to work properly with a sync delegate and an immutable workspace
        //UpdateState(state => {
        //    state.UpdateAsync(items).Wait();
        //    return state;
        //});
    }

    public IMessageDelivery HandleMessage(IMessageDelivery<GetManyRequest<Scenario>> request)
    {
        var data = new Scenario[100];
        Hub.Post(data, o => o.ResponseFor(request));
        return request.Processed();
    }

    private IMessageDelivery HandleGetRequest(IMessageDelivery request)
    {
        var type = request.Message.GetType();
        if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(GetManyRequest<>))
        {
            var elementType = type.GetGenericArguments().First();
            return (IMessageDelivery)GetElementsMethod.MakeGenericMethod(elementType)
                                                      .InvokeAsFunction(this, request);
        }
        return request;
    }

    private static readonly MethodInfo GetElementsMethod
        = ReflectionHelper.GetMethodGeneric<DataPlugin>(x => x.GetElements<object>(null));

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

