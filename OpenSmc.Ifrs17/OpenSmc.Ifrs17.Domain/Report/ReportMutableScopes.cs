using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Export.Factory;
using Systemorph.Vertex.Grid.Model;
using Systemorph.Vertex.Scopes;
using Systemorph.Vertex.Workspace;

//#!import "ReportScopes"


public interface ReportUniverse : IMutableScopeWithStorage<ReportStorage>{}


[InitializeScope(nameof(InitAsync))]
public interface IIfrs17Report : IMutableScope<string, ReportStorage> {
    // Infrastructure
    protected IWorkspace workspace => GetStorage().Workspace;
    protected Systemorph.Vertex.Pivot.Builder.Interfaces.IPivotFactory report => GetStorage().Report;

    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IIfrs17Report>(s => s.WithApplicability<PvReport>(x => x.Identity == nameof(PvReport))
                                              .WithApplicability<RaReport>(x => x.Identity == nameof(RaReport))
                                              .WithApplicability<WrittenReport>(x => x.Identity == nameof(WrittenReport))
                                              .WithApplicability<AccrualReport>(x => x.Identity == nameof(AccrualReport))
                                              .WithApplicability<DeferralReport>(x => x.Identity == nameof(DeferralReport))
                                              .WithApplicability<FcfReport>(x => x.Identity == nameof(FcfReport))
                                              .WithApplicability<ExpAdjReport>(x => x.Identity == nameof(ExpAdjReport))
                                              .WithApplicability<TmReport>(x => x.Identity == nameof(TmReport))
                                              .WithApplicability<CsmReport>(x => x.Identity == nameof(CsmReport))
                                              .WithApplicability<ActLrcReport>(x => x.Identity == nameof(ActLrcReport))
                                              .WithApplicability<LrcReport>(x => x.Identity == nameof(LrcReport))
                                              .WithApplicability<ActLicReport>(x => x.Identity == nameof(ActLicReport))
                                              .WithApplicability<LicReport>(x => x.Identity == nameof(LicReport))
                                              .WithApplicability<FpReport>(x => x.Identity == nameof(FpReport))
                                              .WithApplicability<FpAlternativeReport>(x => x.Identity == nameof(FpAlternativeReport))
                );

     // Basic mutable properties
    (int Year, int Month) ReportingPeriod { get; set; }
    int Year => ReportingPeriod.Year;
    int Month => ReportingPeriod.Month;
    string ReportingNode { get; set; }
    string Scenario { get; set; }  // TODO: Enable dropdown selection including All and Delta
    string Comparison { get; set; }  // TODO: only for scenario at the beginning, meant to enable general purpose comparisons 
    CurrencyType CurrencyType { get; set; }
    string Projection {get; set;} //NB input is P0 or P1 or P15...
    
    private ProjectionConfiguration[] projectionConfigurations => GetStorage().ProjectionConfigurations.SortRelevantProjections(Month);
    private string lastValidProjection => Projection == null ? null : "P" + projectionConfigurations.Select(x => x.SystemName.Split("P")[1]).Take(int.Parse(Projection.Split("P")[1])+1).Last();
    ((int Year, int Month) ReportingPeriod, string ReportingNode, string Scenario, CurrencyType) ShowSettings => (ReportingPeriod, ReportingNode, Scenario, CurrencyType);
    
    // Slice and Dice
    protected string[] forbiddenSlices => new string[] { };

    IEnumerable<string> RowSlices { get; set; }
    protected string[] defaultRowSlices => new string[] { };
    protected string[] rowSlices => RowSlices is null 
                                    ? defaultRowSlices 
                                    : defaultRowSlices.Where(cs => !RowSlices.Contains(cs)).Concat(RowSlices).Where(x => !forbiddenSlices.Contains(x)).ToArray();

    IEnumerable<string> ColumnSlices { get; set; }
    protected string[] defaultColumnSlices => new string[] { };
    protected string[] columnSlices { get{
        var defaultColumnSlicesWithProjection = Projection == null ? defaultColumnSlices : nameof(ReportVariable.Projection).RepeatOnce().Concat(defaultColumnSlices).ToArray();
        var slices = ColumnSlices is null 
                            ? defaultColumnSlicesWithProjection
                            : defaultColumnSlicesWithProjection.Where(cs => !ColumnSlices.Contains(cs)).Concat(ColumnSlices).Where(x => !forbiddenSlices.Contains(x)).ToArray();
        return Scenario == Scenarios.All || Scenario == Scenarios.Delta
            ? slices.Concat(nameof(Scenario).RepeatOnce()).ToArray() 
            : Scenario is null ? slices : nameof(Scenario).RepeatOnce().Concat(slices).ToArray();
    }}

    // Identities
    protected HashSet<(ReportIdentity, CurrencyType)> GetIdentities() => GetStorage().GetIdentities(ReportingPeriod, ReportingNode, Scenario, CurrencyType, lastValidProjection);
    
    // Filter
    IEnumerable<(string filterName, string filterValue)> DataFilter { get; set; }
    protected (string filterName, object filterValue)[] dataFilter => (DataFilter is null ? Enumerable.Empty<(string, object)>() : DataFilter.Select(x => (x.filterName, (object)x.filterValue))).ToArray();

    // Scope Initialization
    async Task InitAsync() {
        var mostRecentPartition = (await workspace.Query<PartitionByReportingNodeAndPeriod>().Where(x => x.Scenario == null).OrderBy(x => x.Year).ThenBy(x => x.Month).ToArrayAsync()).Last();  
        ReportingPeriod = (mostRecentPartition.Year, mostRecentPartition.Month);
        ReportingNode = (await workspace.Query<ReportingNode>().Where(x => x.Parent == null).ToArrayAsync()).First().SystemName; // TODO: change once user permissions are available
        Scenario = null;
        CurrencyType = CurrencyType.Contractual;
        await GetStorage().InitializeReportIndependentCacheAsync();
    }  

    //Report
    public IDataCube<ReportVariable> GetDataCube() => default;
    protected int headerColumnWidthValue => 250;
    public Task<GridOptions> ToReportAsync => GetReportTaskAsync();
    private async Task<GridOptions> GetReportTaskAsync() {
        await GetStorage().InitializeAsync(ReportingPeriod, ReportingNode, Scenario, CurrencyType);   
        return await report.ForDataCube(GetScope<Data>((ReportingPeriod, ReportingNode, Scenario, CurrencyType, Identity, dataFilter)).Cube)
            .WithQuerySource(workspace)
            .SliceRowsBy(rowSlices)
            .SliceColumnsBy(columnSlices)
            .ReportGridOptions(headerColumnWidth: headerColumnWidthValue)
            .ExecuteAsync();
    }
}

public interface PvReport : IIfrs17Report {
    string[] IIfrs17Report.defaultRowSlices => new string[] { "Novelty", "VariableType" };
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency", "EconomicBasis" }; 
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() => 
        DataFilter == null ? GetScopes<LockedBestEstimate>(GetIdentities()).Aggregate().LockedBestEstimate + 
                             GetScopes<CurrentBestEstimate>(GetIdentities()).Aggregate().CurrentBestEstimate
                           : GetScopes<LockedBestEstimate>(GetIdentities()).Aggregate().LockedBestEstimate.Filter(dataFilter) + 
                             GetScopes<CurrentBestEstimate>(GetIdentities()).Aggregate().CurrentBestEstimate.Filter(dataFilter);
}

public interface RaReport : IIfrs17Report {
    string[] IIfrs17Report.forbiddenSlices => new string[] {"AmountType"};
    string[] IIfrs17Report.defaultRowSlices => new string[] { "Novelty", "VariableType" };
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency", "EconomicBasis" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilter == null ? GetScopes<LockedRiskAdjustment>(GetIdentities()).Aggregate().LockedRiskAdjustment + 
                             GetScopes<CurrentRiskAdjustment>(GetIdentities()).Aggregate().CurrentRiskAdjustment
                            : GetScopes<LockedRiskAdjustment>(GetIdentities()).Aggregate().LockedRiskAdjustment.Filter(dataFilter) + 
                              GetScopes<CurrentRiskAdjustment>(GetIdentities()).Aggregate().CurrentRiskAdjustment.Filter(dataFilter);
}

public interface WrittenReport : IIfrs17Report {
    string[] IIfrs17Report.forbiddenSlices => new string[] {nameof(EconomicBasis)};
    string[] IIfrs17Report.defaultRowSlices => new string[] {"AmountType"};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency"};
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilter == null ? GetScopes<WrittenAndAccruals>(GetIdentities()).Aggregate().Written
                            : GetScopes<WrittenAndAccruals>(GetIdentities()).Aggregate().Written.Filter(dataFilter);
}

public interface AccrualReport : IIfrs17Report {
    string[] IIfrs17Report.defaultRowSlices => new string[] {"VariableType"};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency", "EstimateType"};
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() => 
        DataFilter == null ? GetScopes<WrittenAndAccruals>(GetIdentities()).Aggregate().Advance + 
                             GetScopes<WrittenAndAccruals>(GetIdentities()).Aggregate().Overdue
                           : GetScopes<WrittenAndAccruals>(GetIdentities()).Aggregate().Advance.Filter(dataFilter) + 
                             GetScopes<WrittenAndAccruals>(GetIdentities()).Aggregate().Overdue.Filter(dataFilter);
}

public interface DeferralReport : IIfrs17Report {
    string[] IIfrs17Report.defaultRowSlices => new string[] {nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType)};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency"};
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() => 
        DataFilter == null ? GetScopes<Deferrals>(GetIdentities()).Aggregate().Deferrals
                           : GetScopes<Deferrals>(GetIdentities()).Aggregate().Deferrals.Filter(dataFilter);
}

public interface FcfReport : IIfrs17Report {
    string[] IIfrs17Report.defaultRowSlices => new string[] {"Novelty","VariableType"};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency", "EconomicBasis" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>  
        DataFilter == null ? GetScopes<Fcf>(GetIdentities()).Aggregate().Fcf
                           : GetScopes<Fcf>(GetIdentities()).Aggregate().Fcf.Filter(dataFilter);
}

public interface ExpAdjReport : IIfrs17Report {
    string[] IIfrs17Report.defaultRowSlices => new string[] {"AmountType", "EstimateType"};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency"};
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() => 
        DataFilter == null ? GetScopes<ExperienceAdjustment>(GetIdentities()).Aggregate().ActuarialExperienceAdjustment
                           : GetScopes<ExperienceAdjustment>(GetIdentities()).Aggregate().ActuarialExperienceAdjustment.Filter(dataFilter);
}
public interface TmReport : IIfrs17Report {
    string[] IIfrs17Report.forbiddenSlices => new string[] {"AmountType", nameof(EconomicBasis)};
    string[] IIfrs17Report.defaultRowSlices => new string[] {"Novelty", "VariableType"};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() => 
        DataFilter == null ? GetScopes<LrcTechnicalMargin>(GetIdentities()).Aggregate().LrcTechnicalMargin
                           : GetScopes<LrcTechnicalMargin>(GetIdentities()).Aggregate().LrcTechnicalMargin.Filter(dataFilter);
}
public interface CsmReport : IIfrs17Report {
    string[] IIfrs17Report.forbiddenSlices => new string[] {"AmountType", nameof(EconomicBasis)};
    string[] IIfrs17Report.defaultRowSlices => new string[] {"Novelty", "VariableType"};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency", "EstimateType" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() => 
        DataFilter == null ? GetScopes<Csm>(GetIdentities()).Aggregate().Csm + 
                             GetScopes<Lc>(GetIdentities()).Aggregate().Lc + 
                             GetScopes<Loreco>(GetIdentities()).Aggregate().Loreco
                           : GetScopes<Csm>(GetIdentities()).Aggregate().Csm.Filter(dataFilter) + 
                             GetScopes<Lc>(GetIdentities()).Aggregate().Lc.Filter(dataFilter) + 
                             GetScopes<Loreco>(GetIdentities()).Aggregate().Loreco.Filter(dataFilter);
}
public interface ActLrcReport : IIfrs17Report {
    string[] IIfrs17Report.forbiddenSlices => new string[] {"AmountType"};
    string[] IIfrs17Report.defaultRowSlices => new string[] {"Novelty","VariableType"};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency", "EstimateType" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() => 
    DataFilter == null  ? GetScopes<LrcActuarial>(GetIdentities()).Aggregate().LrcActuarial
                        : GetScopes<LrcActuarial>(GetIdentities()).Aggregate().LrcActuarial.Filter(dataFilter);
}
public interface LrcReport : IIfrs17Report {
    string[] IIfrs17Report.forbiddenSlices => new string[] {"AmountType"};
    string[] IIfrs17Report.defaultRowSlices => new string[] {"VariableType"};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency", "EstimateType" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() => 
        DataFilter == null ? GetScopes<Lrc>(GetIdentities()).Aggregate().Lrc
                           : GetScopes<Lrc>(GetIdentities()).Aggregate().Lrc.Filter(dataFilter);
}
public interface ActLicReport : IIfrs17Report {
    string[] IIfrs17Report.forbiddenSlices => new string[] {"AmountType"};
    string[] IIfrs17Report.defaultRowSlices => new string[] {"Novelty","VariableType"};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency", "EstimateType" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() => 
        DataFilter == null ? GetScopes<LicActuarial>(GetIdentities()).Aggregate().LicActuarial
                           : GetScopes<LicActuarial>(GetIdentities()).Aggregate().LicActuarial.Filter(dataFilter);
}
public interface LicReport : IIfrs17Report {
    string[] IIfrs17Report.forbiddenSlices => new string[] {"AmountType"};
    string[] IIfrs17Report.defaultRowSlices => new string[] {"VariableType"};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency", "EstimateType" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() => 
        DataFilter == null ? GetScopes<Lic>(GetIdentities()).Aggregate().Lic
                           : GetScopes<Lic>(GetIdentities()).Aggregate().Lic.Filter(dataFilter);
}
public interface FpReport : IIfrs17Report {
    string[] IIfrs17Report.forbiddenSlices => new string[] {"AmountType", nameof(EconomicBasis)};
    string[] IIfrs17Report.defaultRowSlices => new string[] {"VariableType", "EstimateType"};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency"};
    int IIfrs17Report.headerColumnWidthValue => 500;
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() => 
        DataFilter == null ? GetScopes<FinancialPerformance>(GetIdentities()).Aggregate().FinancialPerformance
                           : GetScopes<FinancialPerformance>(GetIdentities()).Aggregate().FinancialPerformance.Filter(dataFilter);
}

public interface FpAlternativeReport : IIfrs17Report {
    string[] IIfrs17Report.forbiddenSlices => new string[] {"AmountType", nameof(EconomicBasis)};
    string[] IIfrs17Report.defaultRowSlices => new string[] {"VariableType", "EstimateType"};
    string[] IIfrs17Report.defaultColumnSlices => new string[] { "Currency"};
    int IIfrs17Report.headerColumnWidthValue => 500;
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() => 
        DataFilter == null ? GetScopes<FinancialPerformanceAlternative>(GetIdentities()).Aggregate().FinancialPerformanceAlternative
                           : GetScopes<FinancialPerformanceAlternative>(GetIdentities()).Aggregate().FinancialPerformanceAlternative.Filter(dataFilter);
}

public interface Data : IMutableScope<((int year, int month) period, string reportingNode, string scenario, CurrencyType currencyType, 
                                        string reportType, (string filterName, object filterValue)[] dataFilter)> 
{
    IDataCube<ReportVariable> Cube { get{
        var data = GetScope<IIfrs17Report>(Identity.reportType).GetDataCube();
        // TODO: suggestion to place the filter here instead of having it in every applicability scope
        if(Identity.scenario != Scenarios.All && Identity.scenario != Scenarios.Delta) return data;
        if(Identity.scenario == Scenarios.All) return data.Select(x => x.Scenario == null? x with {Scenario = Scenarios.Default } : x).ToDataCube();
        var bestEstimateById = data.Where(x => x.Scenario == null).ToDictionary(x => x.ToIdentityString());
        return data.Select(x => x.Scenario == null ? x with { Scenario = Scenarios.Default } 
                                                   : x with { Value = x.Value - (bestEstimateById.TryGetValue((x with {Scenario = null}).ToIdentityString(), out var be)? be.Value : 0.0) }).ToDataCube();
    }}
} 


public class Ifrs17 
{
    private Systemorph.Vertex.Scopes.Proxy.IScopeFactory scopes;
    private Systemorph.Vertex.Pivot.Builder.Interfaces.IPivotFactory report;
    private IExportVariable export;
    private ReportStorage Storage;
    private ReportUniverse reportUniverse;
    
    //reset
    public void Reset(IWorkspace workspace) => Storage = new ReportStorage(workspace, report, export);

    //constructor
    public Ifrs17 (IWorkspace workspace, 
                   Systemorph.Vertex.Scopes.Proxy.IScopeFactory scopes, 
                   Systemorph.Vertex.Pivot.Builder.Interfaces.IPivotFactory report,
                   IExportVariable export)
    {
        this.scopes = scopes; 
        this.report = report;
        this.export = export;
        Storage = new ReportStorage(workspace, report, export);
        reportUniverse = scopes.ForSingleton().WithStorage(Storage).ToScope<ReportUniverse>();
    }

    public IIfrs17Report PresentValues => reportUniverse.GetScope<IIfrs17Report>(nameof(PvReport));
    public IIfrs17Report RiskAdjustments => reportUniverse.GetScope<IIfrs17Report>(nameof(RaReport));
    public IIfrs17Report WrittenActuals => reportUniverse.GetScope<IIfrs17Report>(nameof(WrittenReport));
    public IIfrs17Report AccrualActuals => reportUniverse.GetScope<IIfrs17Report>(nameof(AccrualReport));
    public IIfrs17Report DeferralActuals => reportUniverse.GetScope<IIfrs17Report>(nameof(DeferralReport));
    public IIfrs17Report FulfillmentCashflows => reportUniverse.GetScope<IIfrs17Report>(nameof(FcfReport));
    public IIfrs17Report ExperienceAdjustments => reportUniverse.GetScope<IIfrs17Report>(nameof(ExpAdjReport));
    public IIfrs17Report TechnicalMargins => reportUniverse.GetScope<IIfrs17Report>(nameof(TmReport));
    public IIfrs17Report AllocatedTechnicalMargins => reportUniverse.GetScope<IIfrs17Report>(nameof(CsmReport));
    public IIfrs17Report ActuarialLrc => reportUniverse.GetScope<IIfrs17Report>(nameof(ActLrcReport));
    public IIfrs17Report Lrc => reportUniverse.GetScope<IIfrs17Report>(nameof(LrcReport));
    public IIfrs17Report ActuarialLic => reportUniverse.GetScope<IIfrs17Report>(nameof(ActLicReport));
    public IIfrs17Report Lic => reportUniverse.GetScope<IIfrs17Report>(nameof(LicReport));
    public IIfrs17Report FinancialPerformance => reportUniverse.GetScope<IIfrs17Report>(nameof(FpReport));
    public IIfrs17Report FinancialPerformanceAlternative => reportUniverse.GetScope<IIfrs17Report>(nameof(FpAlternativeReport));
}



