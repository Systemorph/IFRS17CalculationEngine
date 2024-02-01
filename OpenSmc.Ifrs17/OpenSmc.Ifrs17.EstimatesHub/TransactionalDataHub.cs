using OpenSmc.Activities;
using OpenSmc.Import.Builders;
using OpenSmc.Messaging;
using OpenSmc.ServiceProvider;

namespace OpenSmc.Ifrs17.TransactionalDataHub;

/* For now, this hub owns all data except Dimensions and ReportVariable (subject to change).
 */
public class TransactionalDataHub : MessageHubPlugin<TransactionalDataHub>
{
    [Inject] private IActivityService activityService;

    public TransactionalDataHub(IMessageHub hub, MessageHubConfiguration options) : base(hub)
    {
        options = options.AddImport(x => x);
    }
}



