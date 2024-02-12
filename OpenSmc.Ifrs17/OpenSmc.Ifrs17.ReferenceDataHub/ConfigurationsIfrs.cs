using OpenSmc.Data;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

/* AM (1.2.2024) todo list:
 *  a) organize better the IFRS17 project structure
 *  b) orchestrate which data go to which hub, e.g. refDataHub owns all dimensions
 *      write the financialDataConfiguration for parameters, transactionalData, etc etc
 *  c) finish setting up all model hubs simply by means of this generic DataPlugin
 *  d) implement tests for the DataPlugin by adding tests in the OpenSMC repo
 *  e) test the IFRS17 model hubs writing the level above this configurations
 *      to do this check the existing tests in the OpenSMC, e.g. MessageHubTest
 *      then write the financialDataConfiguration to define routing, addresses, forwarding, etc $
 *  f) think at the viewModelHub, what to do here? where to start?
 *      look at the existing tests in OpenSMC, e.g. LayoutTest
 *  g) monitor the development of the import/export plugin so that we can use them here
 *      in smc v1 
 */



public static class DataHubConfiguration
{
    public static MessageHubConfiguration ConfigurationReferenceDataHub(this MessageHubConfiguration configuration)
    {
        // TODO: this needs to be registered in the higher level
        //var dataSource = financialDataConfiguration.ServiceProvider.GetService<IDataSource>();

        // Make Pluguin available 
        // There should  be a way to get Workspace from plugin

        return configuration.AddData(dc => dc.WithDataSource("ReferenceDataSource",
            ds => ds.WithType<AmountType>(t => t.WithKey(x => x.ExternalId)
                        .WithInitialization(async () => await Task.FromResult(ReferenceData.ReferenceAmountTypes))
                    //.WithUpdate(x => )
                    //.WithDelete()
                )
                .WithType<AocStep>(t => t.WithKey(x => x.AocType)
                    .WithInitialization(async () => await Task.FromResult(ReferenceData.ReferenceAocSteps)))));
        //AddPlugin(hub => new DataPlugin(hub, conf => conf
        //.WithWorkspace(workspace => workspace.WithKey<Currency>()
        //    .WithKey<LineOfBusiness>())
        //.WithPersistence(p => p
        //    .WithDimension<LineOfBusiness>()
        //    .WithDimension<Currency>())));
    }

    //private static DataPersistenceConfiguration WithDimension<T>(this DataPersistenceConfiguration configuration
    //    //IDataSource dataSource
    //)
    //    where T : class, new()
    //{
    //    return configuration.WithType(
    //        () =>
    //            Task.FromResult<IReadOnlyCollection<T>>(new List<T>{}), //await dataSource.Query<T>().ToArrayAsync()
    //        _ => Task.CompletedTask, // dataSource.UpdateAsync(dim),
    //        _ => Task.CompletedTask);
    //    // dataSource.DeleteAsync(dim));
    //}

    //public static Task<IReadOnlyCollection<T>> Initialize<T>(IReadOnlyCollection<T> initData)
    //{
    //    return Task.FromResult(initData);
    //}



    //private static WorkspaceConfiguration WithKey<T>(this WorkspaceConfiguration configuration)
    //    where T : KeyedDimension =>
    //    configuration.Key<T>(x => x.SystemName);

    //private static MessageHubConfiguration WithHandlers<TDim>(this MessageHubConfiguration configuration)
    //where TDim : class, new()
    //{
    //    return configuration.WithHandler<FinancialDimesnionRequest<TDim>>((hub, request) =>
    //    {
    //        hub.Post(new TDim(), o => o.ResponseFor(request));
    //        return request.Processed();
    //    })
    //        .WithHandler<FinancialDimensionManyRequest<TDim>>((hub, request) =>
    //        {
    //            hub.Post(new List<TDim>(), o => o.ResponseFor(request));
    //            return request.Processed();
    //        } );
    //}


    //public static MessageHubConfiguration ConfigurationTransactionalDataHub(this MessageHubConfiguration transactionalHubConfiguration)
    //{
    //    // TODO: this needs to be registered in the higher level
    //    var dataSource = transactionalHubConfiguration.ServiceProvider.GetService<IDataSource>();

    //    return transactionalHubConfiguration
    //        .AddData(data => data.WithWorkspace(w => w)
    //            .WithPersistence(p => p.WithDimension<RawVariable>())); 
    //    /* This is delete of data, not of the hub */
    //    /* Delete of Hub must be implemented separately (pr)*/
    //}

    //public static MessageHubConfiguration ConfigurationViewModelHub(this MessageHubConfiguration transactionalHubConfiguration)
    //{
        // TODO: this needs to be registered in the higher level
        //var dataSource = financialDataConfiguration.ServiceProvider.GetService<IDataSource>();
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

        //return transactionalHubConfiguration;
    //}/*
}


/* Outdated comment
 * 
 * Create request:
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

