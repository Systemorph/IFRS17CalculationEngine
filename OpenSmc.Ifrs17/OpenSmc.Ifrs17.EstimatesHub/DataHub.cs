using OpenSmc.Activities;
using OpenSmc.Import.Builders;
using OpenSmc.Messaging;
using OpenSmc.ServiceProvider;

namespace OpenSmc.Ifrs17.EstimatesHub;

/* For now, this hub owns all data except Dimensions and ReportVariable (subject to change).
 */
public class DataHub : MessageHubPlugin<DataHub>
{
    [Inject] private IActivityService activityService;

    public DataHub(IMessageHub hub, MessageHubConfiguration options) : base(hub)
    {
        options = options.AddImport(x => x);
    }
}



