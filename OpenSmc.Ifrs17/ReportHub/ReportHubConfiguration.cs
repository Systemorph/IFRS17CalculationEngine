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
using static OpenSmc.Ifrs17.ReportHub.ReportScopes;

namespace OpenSmc.Ifrs17.ReportHub;

public static class ReportHubConfiguration
{
    public static MessageHubConfiguration ConfigureReportDataHub(this MessageHubConfiguration configuration)
    {
        var refDataAddress = new ReferenceDataAddress(configuration.Address);
        var dataNodeAddress = new DataNodeAddress(configuration.Address);
        var parameterAddress = new ParameterAddress(configuration.Address);

        // TODO: understand how to configure a generic data hub with non-trivial address without hard-coding the partition
        var ifrsVarAddress = new IfrsVariableAddress(configuration.Address, 2020, 12, "CH", "Bla");
        var reportAddress = new ReportAddress(configuration.Address, 2020, 12, "CH", "Bla");

        return configuration
            .WithServices(services => services.AddSingleton<ScopeFactory>())
            
            .WithHostedHub(refDataAddress, config => config.ConfigureReferenceDataDictInit())
            .WithHostedHub(dataNodeAddress, config => config.ConfigureDataNodeDataDictInit())
            .WithHostedHub(parameterAddress, config => config.ConfigureParameterDataDictInit())

            .WithHostedHub(ifrsVarAddress, config =>
            {
                var address = (IfrsVariableAddress)config.Address;
                return config.ConfigureIfrsDataDictInit(address.Year, address.Month, address.ReportingNode, address.Scenario);
            })
            
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
                    //.AddCustomInitialization(ReportInit(config))
                    ))

            .WithRoutes(route => route.RouteMessage<GetManyRequest<ReportVariable>>(_ => reportAddress));
    }

    public static Action<HubDataSource, ScopeFactory> ReportInit(MessageHubConfiguration config)
    {
        return (workspace, scopeFactory) =>
        {
            // TMP: this is how we retrieve data from this workspace variable
            //workspace.Get<IfrsVariable>();

            var address = (ReportAddress)config.Address;

            // TODO: understand from where to take this currency type
            var currencyType = DataTypes.Constants.Enumerates.CurrencyType.Group;

            var storage = new ReportStorage(workspace);
            //storage.Initialize((address.Year, address.Month), address.ReportingNode, address.Scenario, currencyType);

            using (var universe = scopeFactory.ForSingleton().WithStorage(storage).ToScope<IUniverse>())
            {
                // TODO: take from the scopes the report variables we need
                //universe.GetScopes Identities
                //universe.GetScopes RiskAdjustments

                var reportVariables = new ReportVariable[] { new() { AmountType = "a" }, new() { AmountType = "b" } };

                workspace.Change(new UpdateDataRequest(reportVariables));
            }
        };
    }

    private static Func<ReportVariable, object> ReportVariableKey => x =>
        (x.ReportingNode, x.Scenario, x.Currency, x.Novelty, x.FunctionalCurrency, x.ContractualCurrency, x.GroupOfContract,
         x.Portfolio, x.LineOfBusiness, x.LiabilityType, x.InitialProfitability, x.ValuationApproach, x.AnnualCohort,
         x.OciType, x.Partner, x.IsReinsurance, x.AccidentYear, x.ServicePeriod, x.Projection, x.VariableType, x.Novelty,
         x.AmountType, x.EstimateType, x.EconomicBasis);
}
