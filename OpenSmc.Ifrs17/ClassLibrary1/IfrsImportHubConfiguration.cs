using OpenSmc.Ifrs17.DataNodeHub;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ImportHubs
{
    

    public static class IfrsImportHubConfiguration
    {
        public static string CashflowImportFormat = nameof(CashflowImportFormat);

        public static MessageHubConfiguration ConfigureSimpleDataImportHub(MessageHubConfiguration configuration)
        {
            var refDataNodeAddress = new DataNodeAddress(configuration.Address);
            return configuration.WithHostedHub(refDataNodeAddress, config => config);
        }

    }
}
