using OpenSmc.Collections;
//using OpenSmc.DataCubes;
using OpenSmc.Hierarchies;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Workspace;
using OpenSmc.Domain.Abstractions;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Export.Factory;

namespace OpenSmc.Ifrs17.Domain.Report;

public class ReportStorage {
    protected readonly IWorkspace workspace;
    protected readonly IExportVariable export;
    private readonly IHierarchicalDimensionCache hierarchicalDimensionCache;
    private readonly Systemorph.Vertex.Pivot.Builder.Interfaces.IPivotFactory reportFactory;

    // Current Storage Settings
    public ((int Year, int Month) Period, string ReportingNode, string Scenario, CurrencyType CurrencyType) Args {get; private set;}
    
    // Initial Values for Scopes DropDowns
    public (string DisplayName, string SystemName) InitialReportingNode {get; private set;}
    public (int Year, int Month) InitialReportingPeriod {get; private set;}

    // Aux IData
    private Dictionary<(int year, int month), Dictionary<string, Dictionary<FxPeriod, double>>> exchangeRatesByCurrencyByFxTypeAndPeriod = new(); // IFx Rates
    private Dictionary<(int year, int month), Dictionary<AocStep, FxPeriod>> fxPeriodsByAocStepAndPeriod = new(); // FxParameter
    
    // Dimensions
    public HashSet<string> EstimateTypesWithoutAoc {get; private set;}
    public HashSet<string> TargetScenarios {get; private set;}
    public ProjectionConfiguration[] ProjectionConfigurations {get; private set;}
    private string currentPeriodProjection {get; set;}

    // Variables and Parameters
    private Dictionary<((int year, int month) period, string reportingNode, string scenario), Dictionary<ReportIdentity, Dictionary<string, IDataCube<ReportVariable>>>> variablesDictionary = new();
        
    // Constructor
    public ReportStorage(IWorkspace workspace, 
                        Systemorph.Vertex.Pivot.Builder.Interfaces.IPivotFactory reportFactory, 
                        IExportVariable export) {
        this.workspace = workspace;
        this.hierarchicalDimensionCache = workspace.ToHierarchicalDimensionCache();
        this.reportFactory = reportFactory;
        this.export = export;
    }
    
    // Initializers
    public async Task InitializeReportIndependentCacheAsync() {
        // Hierarchical Dimensions
        await hierarchicalDimensionCache.InitializeAsync<LineOfBusiness>();
        await hierarchicalDimensionCache.InitializeAsync<AmountType>();  
        await hierarchicalDimensionCache.InitializeAsync<VariableType>();
        await hierarchicalDimensionCache.InitializeAsync<ReportingNode>();

        // Initial Values
        var mostRecentPartition = (await workspace.Query<PartitionByReportingNodeAndPeriod>().Where(x => x.Scenario == null).OrderBy(x => x.Year).ThenBy(x => x.Month).ToArrayAsync()).LastOrDefault();
        InitialReportingPeriod = (mostRecentPartition.Year, mostRecentPartition.Month);
        var rootReportingNode = (await workspace.Query<ReportingNode>().Where(x => x.Parent == null).ToArrayAsync()).FirstOrDefault();
        InitialReportingNode = (rootReportingNode.DisplayName, rootReportingNode.SystemName);
    }
    
    public async Task InitializeAsync((int year, int month) period, string? reportingNode, string? scenario, CurrencyType currencyType) {
        // Setting the Args --> Temp for the moment
        Args = (period, reportingNode, scenario, currencyType);
        ProjectionConfigurations = await workspace.Query<ProjectionConfiguration>().ToArrayAsync();
        currentPeriodProjection = ProjectionConfigurations.SortRelevantProjections(period.month).First().SystemName;
        
        EstimateTypesWithoutAoc = (await workspace.Query<EstimateType>().Where(x => x.StructureType == StructureType.None).Select(x => x.SystemName).ToArrayAsync()).ToHashSet();
        TargetScenarios = await GetScenariosAsync(scenario); 
        
        // FX && IFx Parameters
        if(!exchangeRatesByCurrencyByFxTypeAndPeriod.TryGetValue(period, out var exchangeRatesByCurrencyByFxType) || !fxPeriodsByAocStepAndPeriod.TryGetValue(period, out var fxPeriodsByAocStep)) {
            exchangeRatesByCurrencyByFxType = await workspace.GetExchangeRatesDictionaryAsync(period.year, period.month);
            exchangeRatesByCurrencyByFxTypeAndPeriod.Add(period, exchangeRatesByCurrencyByFxType);
            fxPeriodsByAocStep = (await workspace.LoadAocStepConfigurationAsync(period.year, period.month)).Where(x => x.FxPeriod != FxPeriod.NotApplicable).ToDictionary(x => new AocStep(x.AocType, x.Novelty), x => (FxPeriod)x.FxPeriod);
            fxPeriodsByAocStepAndPeriod.Add(period, fxPeriodsByAocStep);
        }
        
        // Variables
        foreach(var rn in GetLeaves<ReportingNode>(reportingNode)) {
            foreach(var scn in TargetScenarios) {
                if(!variablesDictionary.TryGetValue((period, rn, scn), out var variablesByIdentity)) {
                    variablesByIdentity = (await workspace.QueryReportVariablesAsync((period.year, period.month, rn, scn), ProjectionConfigurations.SortRelevantProjections(period.month)))
                        .ToDictionaryGrouped(x => new ReportIdentity {
                            Year = period.year,
                            Month = period.month,
                            ReportingNode = x.ReportingNode,
                            Scenario = scn,
                            Projection = x.Projection,
                            ContractualCurrency = x.ContractualCurrency,
                            FunctionalCurrency = x.FunctionalCurrency,
                            ValuationApproach = x.ValuationApproach,
                            LiabilityType = x.LiabilityType,
                            IsReinsurance = x.IsReinsurance,
                            IsOci = !string.IsNullOrWhiteSpace(x.OciType) },
                                             x => x.ToDictionaryGrouped(y => y.EstimateType,
                                                                        y => y.ToArray().SelectToDataCube(x=> true, x => x)));
            
                   variablesDictionary.Add((period, rn, scn), variablesByIdentity);
                }
            }
        }
    }
    
    // Getters for IData
    public IDataCube<ReportVariable> GetVariables(ReportIdentity reportIdentity, params string[] estimateTypes)
        => (!variablesDictionary.TryGetValue(((reportIdentity.Year, reportIdentity.Month), reportIdentity.ReportingNode, reportIdentity.Scenario), out var variablesByIdentity) || 
            !variablesByIdentity.TryGetValue(reportIdentity, out var variablesByEstimateType))
        ? Enumerable.Empty<ReportVariable>().SelectToDataCube(x => true, x => x)
        : estimateTypes.Length switch {
                0 => variablesByEstimateType.SelectMany(x => x.Value).SelectToDataCube(x => true, x => x),
                1 => variablesByEstimateType.TryGetValue(estimateTypes.First(), out var variables)
                    ? variables.SelectToDataCube(x => true, x => x)
                    : Enumerable.Empty<ReportVariable>().SelectToDataCube(x => true, x => x),
                _ => estimateTypes.Select(et => variablesByEstimateType.TryGetValue(et, out var variables)
                                          ? variables.SelectToDataCube(x => true, x=> x)
                                          : Enumerable.Empty<ReportVariable>())
                    .Aggregate((x, y) => x.Concat(y))
                    .SelectToDataCube(x => true, x=> x)
        };
    
    // Other getters
    public IWorkspace Workspace => workspace;
    public Systemorph.Vertex.Pivot.Builder.Interfaces.IPivotFactory Report => reportFactory;
    public IExportVariable Export => export;

    public IHierarchy<T> GetHierarchy<T>() where T : class, IHierarchicalDimension => hierarchicalDimensionCache.Get<T>();
    
    public HashSet<(ReportIdentity, CurrencyType)> GetIdentities((int year, int month) period, string? reportingNode, string? scenario, CurrencyType currencyType, string? projection = null) {
        var relevantProjection = projection == null ? currentPeriodProjection.RepeatOnce()
                                                    : ProjectionConfigurations.SortRelevantProjections(period.month).TakeWhile(x => x.SystemName != projection ).Select(y => y.SystemName).Concat(projection.RepeatOnce());
        return GetLeaves<ReportingNode>(reportingNode).SelectMany(rn => TargetScenarios.SelectMany(scn =>
        variablesDictionary.TryGetValue((period, rn, scn), out var inner)
                            ? inner.Keys.Where(x => relevantProjection.Contains(x.Projection)).Select(x => (x, currencyType)) 
                            : Enumerable.Empty<(ReportIdentity, CurrencyType)>())).ToHashSet();
    }
    
    public double GetFx((int year, int month) period, string currentCurrency, string targetCurrency, FxPeriod fxPeriod) {
        if (currentCurrency == targetCurrency) return 1;
        if(!exchangeRatesByCurrencyByFxTypeAndPeriod.TryGetValue(period, out var exchangeRatesByCurrencyByFxType))
            throw new Exception ($"No exchange rates for Period {period} were found.");
         return ReportConfigExtensions.GetCurrencyToGroupFx(exchangeRatesByCurrencyByFxType, currentCurrency, fxPeriod, Consts.GroupCurrency)
              / ReportConfigExtensions.GetCurrencyToGroupFx(exchangeRatesByCurrencyByFxType, targetCurrency, fxPeriod, Consts.GroupCurrency);
    }
    
    public FxPeriod GetFxPeriod((int year, int month) period, string projection, string aocType, string novelty) => 
        projection == currentPeriodProjection ? fxPeriodsByAocStepAndPeriod[period][new AocStep(aocType, novelty)] : FxPeriod.EndOfPeriod;
    
    

    // Helpers
    public HashSet<string> GetLeaves<T>(string systemName) where T : class, IHierarchicalDimension {
        var descendants = hierarchicalDimensionCache.Get<T>(systemName).Descendants(includeSelf: true);
        return descendants.Where(x => !descendants.Select(y => y.Parent).Contains(x.SystemName)).Select(x => x.SystemName).ToHashSet();
    }

    public async Task<HashSet<string>> GetScenariosAsync(string scenario) => 
        scenario == Scenarios.Delta || scenario == Scenarios.All
        ? (await workspace.Query<PartitionByReportingNodeAndPeriod>().Select(x => x.Scenario).ToArrayAsync()).ToHashSet()
        : scenario.RepeatOnce().ToHashSet();
}
