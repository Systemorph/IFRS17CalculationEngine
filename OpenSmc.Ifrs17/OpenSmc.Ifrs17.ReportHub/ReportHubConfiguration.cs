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
using OpenSmc.Pivot.Builder;
using OpenSmc.DataCubes;
using OpenSmc.Reporting.Builder;
using OpenSmc.Scopes;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;
using static OpenSmc.Ifrs17.ReportHub.ReportScopes;

namespace OpenSmc.Ifrs17.ReportHub;

public static class ReportHubConfiguration
{
    public static MessageHubConfiguration ConfigureReportDataHub(this MessageHubConfiguration configuration)
    {
        var refDataAddress = new ReferenceDataAddress(configuration.Address);
        var dataNodeAddress = new DataNodeAddress(configuration.Address);
        var parameterAddress = new ParameterDataAddress(configuration.Address);
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
                        // Here eventually we can decide whether the default is all types available (12.3.24, AM)
                        .FromHub(refDataAddress, ds => ds
                            .WithType<AmountType>().WithType<DeferrableAmountType>().WithType<AocConfiguration>()
                            .WithType<AocType>().WithType<StructureType>().WithType<CreditRiskRating>().WithType<Currency>().WithType<EconomicBasis>()
                            .WithType<EstimateType>().WithType<LiabilityType>().WithType<LineOfBusiness>().WithType<Profitability>()
                            .WithType<Novelty>().WithType<OciType>().WithType<Partner>().WithType<PnlVariableType>().WithType<RiskDriver>()
                            .WithType<Scenario>().WithType<ValuationApproach>().WithType<ProjectionConfiguration>().WithType<ReportingNode>()
                            .WithType<PartitionByReportingNode>().WithType<PartitionByReportingNodeAndPeriod>())
                        .FromHub(dataNodeAddress, ds => ds
                            .WithType<Portfolio>().WithType<GroupOfContract>()
                            .WithType<InsurancePortfolio>().WithType<GroupOfInsuranceContract>()
                            .WithType<ReinsurancePortfolio>().WithType<GroupOfReinsuranceContract>())
                        .FromHub(parameterAddress, ds => ds
                            .WithType<ExchangeRate>())
                        .FromHub(ifrsVarAddress, ds => ds
                            .WithType<IfrsVariable>()),
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

            // TODO: understand from where to take this currency type (6.3.24, AM)
            var currencyType = DataTypes.Constants.Enumerates.CurrencyType.Group;

            var a = workspace.GetData<AmountType>();

            var storage = new ReportStorage(workspace);
            storage.Initialize((address.Year, address.Month), address.ReportingNode, address.Scenario, currencyType);
            
            var res = Enumerable.Empty<ReportVariable>(); 
            using (var universe = scopeFactory.ForSingleton().WithStorage(storage).ToScope<IUniverse>())
            {
                // TODO: here the identities are 0
                var ids = storage.GetIdentities((address.Year, address.Month), address.ReportingNode, address.Scenario, currencyType);

                // TODO: exception thrown here: cannot convert to output type IDataCube
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
