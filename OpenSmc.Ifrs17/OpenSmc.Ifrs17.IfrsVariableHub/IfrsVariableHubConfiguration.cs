using OpenSmc.Activities;
using OpenSmc.Messaging;
using OpenSmc.ServiceProvider;

namespace OpenSmc.Ifrs17.IfrsVariableHub;

/* For now, this hub owns all data except Dimensions and ReportVariable (subject to change).
 */
public static class IfrsVariableHubConfiguration
{
    public static MessageHubConfiguration ConfigureIfrsDataDictInit(this MessageHubConfiguration configuration) =>
        configuration;

}



