using OpenSmc.Data;
using OpenSmc.Messaging;
using OpenSmc.Reporting;
using OpenSmc.Ifrs17.DataNodeHub;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Ifrs17.ParameterDataHub;
using OpenSmc.Ifrs17.IfrsVariableHub;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Scopes.Proxy;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using static OpenSmc.Ifrs17.ReportHub.ReportScopes;
using OpenSmc.Pivot.Builder;
using OpenSmc.DataCubes;
using OpenSmc.Reporting.Builder;
using OpenSmc.Scopes;
using DocumentFormat.OpenXml.Bibliography;

namespace OpenSmc.Ifrs17.ReportHub;

public static class ReportHubConfiguration
{
    public static MessageHubConfiguration ConfigureReportDataHub(this MessageHubConfiguration configuration)
    {
        var refDataAddress = new ReferenceDataAddress(configuration.Address);
        var dataNodeAddress = new DataNodeAddress(configuration.Address);
        var parameterAddress = new ParameterAddress(configuration.Address);
        var ifrsVarAddress = new IfrsVariableAddress(configuration.Address, 2020, 12, "CH", null);
        var reportAddress = new ReportAddress(configuration.Address, 2020, 12, "CH", null);

        return configuration
            .WithServices(services => services.RegisterScopes())

            .WithHostedHub(refDataAddress, config => config.ConfigureReferenceDataDictInit())
            .WithHostedHub(dataNodeAddress, config => config.ConfigureDataNodeDataDictInit())
            .WithHostedHub(parameterAddress, config => config.ConfigureParameterDataDictInit())

            .WithHostedHub(ifrsVarAddress, config =>
            {
                var address = (IfrsVariableAddress)config.Address;
                return config.ConfigureIfrsDataDictInit(address.Year, address.Month, address.ReportingNode, address.Scenario);
            })

            .WithHostedHub(reportAddress, config => config
                .AddReporting(
                    data => data
                        // Here I am not specifying any type under the assumption that it will synch all types available, pls check (12.3.24, AM)
                        .FromHub(refDataAddress, ds => ds)
                        .FromHub(dataNodeAddress, ds => ds
                            .WithType<InsurancePortfolio>().WithType<GroupOfInsuranceContract>()
                            .WithType<ReinsurancePortfolio>().WithType<GroupOfReinsuranceContract>())
                        .FromHub(parameterAddress, ds => ds
                            .WithType<ExchangeRate>()),
                    reportConfig => reportConfig
                        .WithDataCubeOn(GetDataCube(config), GetReportFunc())));
    }

    private static Func<DataCubePivotBuilder<IDataCube<ReportVariable>, ReportVariable, ReportVariable, ReportVariable>, ReportRequest,
        DataCubeReportBuilder<IDataCube<ReportVariable>, ReportVariable, ReportVariable, ReportVariable>> 
        GetReportFunc()
    {
        return (reportBuilder, reportRequest) => reportBuilder
                    .SliceRowsBy(nameof(AmountType))
                    .ToTable()
                    //.WithOptions(rm => rm.HideRowValuesForDimension("DimA", x => x.ForLevel(1)))
                    .WithOptions(o => o.AutoHeight());
    }

    public static Func<IWorkspace, IScopeFactory, ReportRequest, IEnumerable<ReportVariable>> GetDataCube(MessageHubConfiguration config)
    {
        return (workspace, scopeFactory, reportRequest) =>
        {
            var address = (ReportAddress)config.Address;

            // TODO: understand from where to take this currency type
            var currencyType = DataTypes.Constants.Enumerates.CurrencyType.Group;

            var storage = new ReportStorage(workspace);
            storage.Initialize((address.Year, address.Month), address.ReportingNode, address.Scenario, currencyType);
            
            var res = Enumerable.Empty<ReportVariable>(); 
            using (var universe = scopeFactory.ForSingleton().WithStorage(storage).ToScope<IUniverse>())
            {
                var ids = storage.GetIdentities((address.Year, address.Month), address.ReportingNode, address.Scenario, currencyType);

                var pvs = universe.GetScopes<LockedBestEstimate>(ids).Select(x => x.LockedBestEstimate).Aggregate() +
                          universe.GetScopes<CurrentBestEstimate>(ids).Select(x => x.CurrentBestEstimate).Aggregate() +
                          universe.GetScopes<NominalBestEstimate>(ids).Select(x => x.NominalBestEstimate).Aggregate();

                res = res.Concat(pvs.ToArray());
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
