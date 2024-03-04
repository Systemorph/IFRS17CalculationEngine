using OpenSmc.Data;
using OpenSmc.Messaging;
using OpenSmc.Ifrs17.DataNodeHub;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Ifrs17.ParameterDataHub;
using OpenSmc.Ifrs17.IfrsVariableHub;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Data.Persistence;
using OpenSmc.Scopes.Proxy;
using Microsoft.Extensions.DependencyInjection;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

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
                    .FromConfigurableDataSource("ReportDataSource", ds => ds
                        .WithType<ReportVariable>(t => t.WithKey(ReportVariableKey)))
                    .FromHub(refDataAddress, ds => ds
                        // TODO: complete this list
                        .WithType<AmountType>().WithType<LineOfBusiness>())
                    .FromHub(dataNodeAddress, ds => ds
                        .WithType<InsurancePortfolio>().WithType<GroupOfInsuranceContract>()
                        .WithType<ReinsurancePortfolio>().WithType<GroupOfReinsuranceContract>())
                    .FromHub(parameterAddress, ds => ds 
                        .WithType<ExchangeRate>().WithType<CreditDefaultRate>().WithType<PartnerRating>())
                    .FromHub(ifrsVarAddress, ds => ds
                        .WithType<IfrsVariable>())
                    .AddCustomInitialization(ReportInit(config))));
    }

    public static Action<HubDataSource, ScopeFactory> ReportInit(MessageHubConfiguration config)
    {
        return (workspace, scopeFactory) =>
        {
            //workspace.GetData<IfrsVariable>();
            
            // TODO: WIP
            var storage = new ReportStorage(workspace);
            storage.Initialize((2020, 12), "CH", null, DataTypes.Constants.Enumerates.CurrencyType.Group);

            // TODO: instantiate a new scope registry here
            //scopeFactory.ForSingleton<>();

            // TODO
        };
    }

    private static Func<ReportVariable, object> ReportVariableKey => x =>
        (x.ReportingNode, x.Scenario, x.Currency, x.Novelty, x.FunctionalCurrency, x.ContractualCurrency, x.GroupOfContract,
         x.Portfolio, x.LineOfBusiness, x.LiabilityType, x.InitialProfitability, x.ValuationApproach, x.AnnualCohort,
         x.OciType, x.Partner, x.IsReinsurance, x.AccidentYear, x.ServicePeriod, x.Projection, x.VariableType, x.Novelty,
         x.AmountType, x.EstimateType, x.EconomicBasis);
}
