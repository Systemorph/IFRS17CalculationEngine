using OpenSmc.Activities;
using OpenSmc.Messaging;
using OpenSmc.ServiceProvider;

namespace OpenSmc.Ifrs17.IfrsVariableHub;

/* For now, this hub owns all data except Dimensions and ReportVariable (subject to change).
 */
public class IfrsVariableHub : MessageHubPlugin<IfrsVariableHub>
{
    [Inject] private IActivityService activityService;

    public IfrsVariableHub(IMessageHub hub, MessageHubConfiguration options) : base(hub)
    {
        //options = options.AddImport(x => x);
    }
}



