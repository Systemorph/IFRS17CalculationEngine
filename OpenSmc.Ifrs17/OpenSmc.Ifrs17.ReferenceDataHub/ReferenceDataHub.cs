using OpenSmc.Activities;
using OpenSmc.Import.Builders;
using OpenSmc.Messaging;
using OpenSmc.ServiceProvider;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

public class ReferenceDataHub : MessageHubPlugin<ImportPlugin>
{
    [Inject] private IActivityService activityService;

    public ReferenceDataHub(IMessageHub hub, MessageHubConfiguration options) : base(hub)
    {
        options = options.AddImport(x => x);
    }
}
