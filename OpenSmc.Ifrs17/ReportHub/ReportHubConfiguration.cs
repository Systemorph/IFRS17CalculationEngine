using OpenSmc.Data;
using OpenSmc.Messaging;
using OpenSmc.Import;
using OpenSmc.Ifrs17.DataNodeHub;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Ifrs17.ParameterDataHub;
using OpenSmc.Ifrs17.IfrsVariableHub;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.Utils;
using OpenSmc.Data.Persistence;
using OpenSmc.Scopes.Proxy;
using Microsoft.Extensions.DependencyInjection;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;

namespace OpenSmc.Ifrs17.ReportHub;

public static class ReportHubConfiguration
{
    public static MessageHubConfiguration ConfigureReportDataHub(this MessageHubConfiguration configuration)
    {
        var refDataAddress = new ReferenceDataAddress(configuration.Address);
        var dataNodeAddress = new DataNodeAddress(configuration.Address);
        var parameterAddress = new ParameterAddress(configuration.Address);
        var ifrsVarAddress = new IfrsVariableAddress(configuration.Address);
        var reportAddress = new ReportAddress(configuration.Address);

        return configuration
            .WithServices(services => services.AddSingleton<ScopeFactory>())
            
            .WithHostedHub(refDataAddress, config => config.ConfigureReferenceDataDictInit())
            .WithHostedHub(dataNodeAddress, config => config.ConfigureDataNodeDataDictInit())
            .WithHostedHub(parameterAddress, config => config.ConfigureParameterDataDictInit())
            .WithHostedHub(ifrsVarAddress, config => config.ConfigureIfrsDataDictInit(2020, 12, "CH", default)) // TODO: ???
            
            .WithHostedHub(reportAddress, config => config
                .AddData(data => data
                    .FromHub(refDataAddress, ds => ds
                        .WithType<AmountType>().WithType<LineOfBusiness>())
                    .FromHub(ifrsVarAddress, ds => ds
                        .WithType<IfrsVariable>())
                    .AddCustomInitialization(ReportInit(config))));
    }

    public static Action<HubDataSource, ScopeFactory> ReportInit(MessageHubConfiguration config)
    {
        return (workspace, scopeFactory) =>
        {
            // TODO: WIP
            var storage = new ReportStorage(workspace);
            storage.Initialize((2020, 12), "CH", null, DataTypes.Constants.Enumerates.CurrencyType.Group);
        };
    }
}
