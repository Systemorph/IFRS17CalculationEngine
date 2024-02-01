using OpenSmc.Activities;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Import.Builders;
using OpenSmc.Messaging;
using OpenSmc.ServiceProvider;

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

public class ReferenceDataHub : MessageHubPlugin<ReferenceDataHub>
{
    [Inject] private IActivityService activityService;

    public ReferenceDataHub(IMessageHub hub, MessageHubConfiguration options) : base(hub)
    {
        options = options
            .AddImport(x => x)
            .WithBuildupAction(x => { })
            .WithHandler<GetScenarioRequest>((hub, request) =>
            {
                hub.Post(new ScenarioData(new Scenario[0]), options => options.ResponseFor(request));
                return request.Processed();
            });
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

