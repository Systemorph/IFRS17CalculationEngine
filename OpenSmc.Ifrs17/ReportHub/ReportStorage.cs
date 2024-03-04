﻿using OpenSmc.Collections;
using OpenSmc.Data;
using OpenSmc.DataCubes;
using OpenSmc.Domain.Abstractions;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Hierarchies;

namespace OpenSmc.Ifrs17.ReportHub;

public class ReportStorage
{
    public IWorkspace Workspace { get; }
    //public IExportVariable Export { get; }
    public IHierarchicalDimensionCache HierarchicalDimensionCache { get; }
    //public IPivotFactory ReportFactory { get; }

    // Current Storage Settings
    public ((int Year, int Month) Period, string ReportingNode, string Scenario, CurrencyType CurrencyType) Args { get; private set; }

    // Initial Values for Scopes DropDowns
    public (string DisplayName, string SystemName) InitialReportingNode { get; private set; }
    public (int Year, int Month) InitialReportingPeriod { get; private set; }

    // Aux Data
    private Dictionary<(int year, int month), Dictionary<string, Dictionary<FxPeriod, double>>> exchangeRatesByCurrencyByFxTypeAndPeriod = new(); // Fx Rates
    private Dictionary<(int year, int month), Dictionary<AocStep, FxPeriod>> fxPeriodsByAocStepAndPeriod = new(); // FxParameter

    // Dimensions
    public HashSet<string> EstimateTypesWithoutAoc { get; private set; }
    public HashSet<string?> TargetScenarios { get; private set; }
    public ProjectionConfiguration[] ProjectionConfigurations { get; private set; }
    private string currentPeriodProjection { get; set; }

    // Variables and Parameters
    private Dictionary<((int year, int month) period, string reportingNode, string scenario), Dictionary<ReportIdentity, Dictionary<string, IDataCube<ReportVariable>>>> variablesDictionary = new();

    // Constructor
    public ReportStorage(IWorkspace workspace)
                        //IPivotFactory reportFactory,
                        //IExportVariable export)
    {
        Workspace = workspace;
        //HierarchicalDimensionCache = workspace.ToHierarchicalDimensionCache();
        //ReportFactory = reportFactory;
        //Export = export;
    }

    // Initializers
    public async Task InitializeReportIndependentCacheAsync()
    {
        // Hierarchical Dimensions
        await HierarchicalDimensionCache.InitializeAsync<LineOfBusiness>();
        await HierarchicalDimensionCache.InitializeAsync<AmountType>();
        await HierarchicalDimensionCache.InitializeAsync<VariableType>();
        await HierarchicalDimensionCache.InitializeAsync<ReportingNode>();

        // Initial Values
        var mostRecentPartition = Workspace.GetData<PartitionByReportingNodeAndPeriod>().Where(x => x.Scenario == null).OrderBy(x => x.Year).ThenBy(x => x.Month).LastOrDefault();
        InitialReportingPeriod = (mostRecentPartition?.Year?? 0, mostRecentPartition?.Month?? 0);
        var rootReportingNode = Workspace.GetData<ReportingNode>().FirstOrDefault(x => x.Parent == null);
        InitialReportingNode = (rootReportingNode?.DisplayName?? "", rootReportingNode?.SystemName?? "");
    }

    public void Initialize((int year, int month) period, string reportingNode, string scenario, CurrencyType currencyType)
    {
        // Setting the Args --> Temp for the moment
        Args = (period, reportingNode, scenario, currencyType);
        ProjectionConfigurations = Workspace.GetData<ProjectionConfiguration>().ToArray();
        currentPeriodProjection = ProjectionConfigurations.SortRelevantProjections(period.month).First().SystemName;

        EstimateTypesWithoutAoc = Workspace.GetData<EstimateType>().Where(x => x.StructureType != EstimateTypeStructureTypes.AoC).Select(x => x.SystemName).ToArray().ToHashSet();
        TargetScenarios = GetScenarios(scenario);

        // FX && Fx Parameters
        if (!exchangeRatesByCurrencyByFxTypeAndPeriod.TryGetValue(period, out var exchangeRatesByCurrencyByFxType) || !fxPeriodsByAocStepAndPeriod.TryGetValue(period, out var fxPeriodsByAocStep))
        {
            exchangeRatesByCurrencyByFxType = Workspace.GetExchangeRatesDictionary(period.year, period.month);
            exchangeRatesByCurrencyByFxTypeAndPeriod.Add(period, exchangeRatesByCurrencyByFxType);
            fxPeriodsByAocStep = Workspace.LoadAocStepConfiguration(period.year, period.month).Where(x => x.FxPeriod != FxPeriod.NotApplicable).ToDictionary(x => new AocStep(x.AocType, x.Novelty), x => (FxPeriod)x.FxPeriod);
            fxPeriodsByAocStepAndPeriod.Add(period, fxPeriodsByAocStep);
        }

        // Variables
        foreach (var rn in GetLeaves<ReportingNode>(reportingNode))
        {
            foreach (var scn in TargetScenarios)
            {
                if (!variablesDictionary.TryGetValue((period, rn, scn), out var variablesByIdentity))
                {
                    variablesByIdentity = Workspace.QueryReportVariables((period.year, period.month, rn, scn), ProjectionConfigurations.SortRelevantProjections(period.month))
                        .ToDictionaryGrouped(x => new ReportIdentity
                        {
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
                            IsOci = !string.IsNullOrWhiteSpace(x.OciType)
                        },
                                             x => x.ToDictionaryGrouped(y => y.EstimateType,
                                                                        y => y.ToArray().ToDataCube()));

                    variablesDictionary.Add((period, rn, scn), variablesByIdentity);
                }
            }
        }
    }

    // Getters for Data
    public IDataCube<ReportVariable> GetVariables(ReportIdentity reportIdentity, params string[] estimateTypes)
    {
        return (!variablesDictionary.TryGetValue(((reportIdentity.Year, reportIdentity.Month), reportIdentity.ReportingNode, reportIdentity.Scenario), out var variablesByIdentity) ||
                !variablesByIdentity.TryGetValue(reportIdentity, out var variablesByEstimateType))
            ? Enumerable.Empty<ReportVariable>().ToDataCube()
            : estimateTypes.Length switch
            {
                0 => variablesByEstimateType.SelectMany(x => x.Value).ToDataCube(),
                1 => variablesByEstimateType.TryGetValue(estimateTypes.First(), out var variables)
                    ? variables.ToDataCube()
                    : Enumerable.Empty<ReportVariable>().ToDataCube(),
                _ => estimateTypes.Select(et => variablesByEstimateType.TryGetValue(et, out var variables)
                                          ? variables.ToDataCube()
                                          : Enumerable.Empty<ReportVariable>())
                    .Aggregate((x, y) => x.Concat(y))
                    .ToDataCube()
            };
    }

    public IHierarchy<T> GetHierarchy<T>() where T : class, IHierarchicalDimension => HierarchicalDimensionCache.Get<T>();

    public HashSet<(ReportIdentity, CurrencyType)> GetIdentities((int year, int month) period, string reportingNode, string scenario, CurrencyType currencyType, string projection = null)
    {
        var relevantProjection = projection == null ? ProjectionConfigurations.SortRelevantProjections(period.month).First().SystemName.RepeatOnce()
                                                    : ProjectionConfigurations.SortRelevantProjections(period.month).TakeWhile(x => x.SystemName != projection).Select(y => y.SystemName).Concat(projection.RepeatOnce());
        return GetLeaves<ReportingNode>(reportingNode).SelectMany(rn => TargetScenarios.SelectMany(scn =>
        variablesDictionary.TryGetValue((period, rn, scn), out var inner)
                            ? inner.Keys.Where(x => relevantProjection.Contains(x.Projection)).Select(x => (x, currencyType))
                            : Enumerable.Empty<(ReportIdentity, CurrencyType)>())).ToHashSet();
    }

    public double GetFx((int year, int month) period, string currentCurrency, string targetCurrency, FxPeriod fxPeriod)
    {
        if (currentCurrency == targetCurrency) return 1;
        if (!exchangeRatesByCurrencyByFxTypeAndPeriod.TryGetValue(period, out var exchangeRatesByCurrencyByFxType))
            throw new Exception($"No exchange rates for Period {period} were found.");
        return ReportStorageExtensions.GetCurrencyToGroupFx(exchangeRatesByCurrencyByFxType, currentCurrency, fxPeriod, BusinessConstant.GroupCurrency)
               / ReportStorageExtensions.GetCurrencyToGroupFx(exchangeRatesByCurrencyByFxType, targetCurrency, fxPeriod, BusinessConstant.GroupCurrency);
    }

    public FxPeriod GetFxPeriod((int year, int month) period, string projection, string aocType, string novelty) =>
        projection == currentPeriodProjection ? fxPeriodsByAocStepAndPeriod[period][new AocStep(aocType, novelty)] : FxPeriod.EndOfPeriod;

    // Helpers
    public HashSet<string> GetLeaves<T>(string systemName) where T : class, IHierarchicalDimension
    {
        var descendants = HierarchicalDimensionCache.Get<T>(systemName).Descendants(includeSelf: true);
        return descendants.Where(x => !descendants.Select(y => y.Parent).Contains(x.SystemName)).Select(x => x.SystemName).ToHashSet();
    }

    public HashSet<string?> GetScenarios(string scenario) =>
        scenario == Scenarios.Delta || scenario == Scenarios.All
        ? Workspace.GetData<PartitionByReportingNodeAndPeriod>().Select(x => x.Scenario).ToArray().ToHashSet()
        : scenario.RepeatOnce().ToHashSet() as HashSet<string?>;
}