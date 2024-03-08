using OpenSmc.Data;
using OpenSmc.Messaging;
using OpenSmc.Reporting;
using OpenSmc.Ifrs17.DataNodeHub;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Ifrs17.ParameterDataHub;
using OpenSmc.Ifrs17.IfrsVariableHub;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Scopes.Proxy;
using Microsoft.Extensions.DependencyInjection;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using static OpenSmc.Ifrs17.ReportHub.ReportScopes;
using OpenSmc.Pivot.Builder;
using OpenSmc.DataCubes;
using OpenSmc.Reporting.Builder;

namespace OpenSmc.Ifrs17.ReportHub;

public static class ReportHubConfiguration
{
    public static MessageHubConfiguration ConfigureReportDataHub(this MessageHubConfiguration configuration)
    {
        var refDataAddress = new ReferenceDataAddress(configuration.Address);
        var dataNodeAddress = new DataNodeAddress(configuration.Address);
        var parameterAddress = new ParameterAddress(configuration.Address);
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
                .AddReporting(data => data // this will become the Report Plugin
                    .FromConfigurableDataSource("ReportDataSource", ds => ds
                        .WithType<ReportVariable>(t => t.WithKey(ReportVariableKey)))
                    .FromHub(refDataAddress, ds => ds
                        // TODO: complete this list
                        .WithType<AmountType>().WithType<LineOfBusiness>())
                    .FromHub(dataNodeAddress, ds => ds
                        .WithType<InsurancePortfolio>().WithType<GroupOfInsuranceContract>()
                        .WithType<ReinsurancePortfolio>().WithType<GroupOfReinsuranceContract>())
                    .FromHub(parameterAddress, ds => ds 
                        .WithType<ExchangeRate>().WithType<CreditDefaultRate>().WithType<PartnerRating>()),

                    reportConfig => reportConfig
                    .WithDataCubeOn(GetDataCube(config), GetReportFunc(reportConfig)))

            .WithRoutes(route => route.RouteMessage<GetManyRequest<ReportVariable>>(_ => reportAddress)));
    }

    private static Func<DataCubePivotBuilder<IDataCube<ReportVariable>, ReportVariable, ReportVariable, ReportVariable>, 
        DataCubeReportBuilder<IDataCube<ReportVariable>, ReportVariable, ReportVariable, ReportVariable>> 
        GetReportFunc(ReportConfiguration reportConfig)
    {
        throw new NotImplementedException();
    }

    public static Func<IWorkspace, IScopeFactory, IEnumerable<ReportVariable>> GetDataCube(MessageHubConfiguration config)
    {
        return (workspace, scopeFactory) =>
        {
            // TMP: this is how we retrieve data from this workspace variable
            //workspace.Get<IfrsVariable>();

            var address = (ReportAddress)config.Address;

            // TODO: understand from where to take this currency type
            var currencyType = DataTypes.Constants.Enumerates.CurrencyType.Group;

            var storage = new ReportStorage(workspace);
            storage.Initialize((address.Year, address.Month), address.ReportingNode, address.Scenario, currencyType);

            IEnumerable<ReportVariable> res; 

            using (var universe = scopeFactory.ForSingleton().WithStorage(storage).ToScope<IUniverse>())
            {
                // TODO: take from the scopes the report variables we need
                //universe.GetScopes Identities
                //universe.GetScopes RiskAdjustments

                res = new ReportVariable[] { new() { AmountType = "a" }, new() { AmountType = "b" } };
            }

            return res;
        };
    }

    private static Func<ReportVariable, object> ReportVariableKey => x =>
        (x.ReportingNode, x.Scenario, x.Currency, x.Novelty, x.FunctionalCurrency, x.ContractualCurrency, x.GroupOfContract,
         x.Portfolio, x.LineOfBusiness, x.LiabilityType, x.InitialProfitability, x.ValuationApproach, x.AnnualCohort,
         x.OciType, x.Partner, x.IsReinsurance, x.AccidentYear, x.ServicePeriod, x.Projection, x.VariableType, x.Novelty,
         x.AmountType, x.EstimateType, x.EconomicBasis);
}
