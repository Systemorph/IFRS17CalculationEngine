using OpenSmc.Collections;
using OpenSmc.Data;
using OpenSmc.Domain.Abstractions;
using OpenSmc.Hierarchies;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.Constants.Validations;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.Args;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;

namespace OpenSmc.Ifrs17.CalculationScopes;

public class ImportStorage
{
    public readonly IWorkspace Workspace; 
    private readonly HierarchicalDimensionCache _hierarchyCache;
    private readonly ImportArgs _args;

    //Format
    public string ImportFormat => _args.ImportFormat;

    //Time Periods 
    public (int Year, int Month) CurrentReportingPeriod => (_args.Year, _args.Month);
    public (int Year, int Month) PreviousReportingPeriod => (_args.Year - 1, Consts.MonthInAYear); // YTD Logic

    List<string> Warnings { get; } = new List<string>();

    //Projections
    private ProjectionConfiguration[] ProjectionConfiguration;
    public int FirstNextYearProjection;

    //DataNodes
    public IDictionary<string, DataNodeData> DataNodeDataBySystemName { get; private set; }
    public IDictionary<ImportScope, HashSet<string>> DataNodesByImportScope { get; private set; }
    public IDictionary<string, ICollection<int?>> AccidentYearsByDataNode { get; private set; }

    //Variables
    public IDictionary<string, ICollection<RawVariable>> RawVariablesByImportIdentity { get; private set; }
    public IDictionary<string, ICollection<IfrsVariable>> IfrsVariablesByImportIdentity { get; private set; }

    //Parameters
    public Dictionary<string, YieldCurve> LockedInYieldCurve { get; private set; }
    public Dictionary<string, Dictionary<int, YieldCurve>> CurrentYieldCurve { get; private set; }
    public Dictionary<int, Dictionary<string, PartnerRating>> LockedInPartnerRating { get; private set; }
    public Dictionary<string, Dictionary<int, PartnerRating>> CurrentPartnerRating { get; private set; }
    public Dictionary<int, Dictionary<string, CreditDefaultRate>> LockedInCreditDefaultRates { get; private set; }
    public Dictionary<string, Dictionary<int, CreditDefaultRate>> CurrentCreditDefaultRates { get; private set; }
    public Dictionary<string, Dictionary<int, SingleDataNodeParameter>> SingleDataNodeParametersByGoc { get; private set; }
    public Dictionary<string, Dictionary<int, HashSet<InterDataNodeParameter>>> InterDataNodeParametersByGoc { get; private set; }
    public Dictionary<AocStep, AocConfiguration> AocConfigurationByAocStep { get; private set; }

    private Dictionary<StructureType, HashSet<AocStep>> aocStepByStructureType;

    //Dimensions -- natural to get rid of dictionaries
    public Dictionary<string, AmountType> AmountTypeDimension { get; private set; }
    public Dictionary<string, Novelty> NoveltyDimension { get; private set; }
    public Dictionary<string, EstimateType> EstimateTypeDimension { get; private set; }
    public Dictionary<string, HashSet<string>> EstimateTypesByImportFormat { get; private set; }

    //Constructor
    public ImportStorage(ImportArgs args, IWorkspace workspace)
    {
        Workspace = workspace;
        _args = args;
        _hierarchyCache = new HierarchicalDimensionCache(workspace);
    }

    //Periods
    public ValuationPeriod GetValuationPeriod(ImportIdentity id) => AocConfigurationByAocStep[new AocStep(id.AocType, id.Novelty)].ValuationPeriod;
    public PeriodType GetYieldCurvePeriod(ImportIdentity id) => AocConfigurationByAocStep[new AocStep(id.AocType, id.Novelty)].YcPeriod;
    public PeriodType GetCreditDefaultRiskPeriod(ImportIdentity id) => AocConfigurationByAocStep[new AocStep(id.AocType, id.Novelty)].CdrPeriod;

    public IEnumerable<AocStep> GetAllAocSteps(StructureType structureTypes) => aocStepByStructureType[structureTypes];

    public IEnumerable<AocStep> GetCalculatedTelescopicAocSteps() => AocConfigurationByAocStep.Where(kvp => kvp.Value.DataType == DataType.CalculatedTelescopic).Select(kvp => kvp.Key);

    public DataNodeData GetDataNodeData(ImportIdentity id) => DataNodeDataBySystemName[id.DataNode];

    public SingleDataNodeParameter GetSingleDataNodeParameter(ImportIdentity id, int period) => SingleDataNodeParametersByGoc.TryGetValue(id.DataNode, out var singleDataNodeParameter)
            ? singleDataNodeParameter[period] : throw new Exception(Error.MissingSingleDataNodeParameter.GetMessage(id.DataNode));

    public (int Year, int Month) GetReportingPeriod(int period) => period == Consts.CurrentPeriod ?
                                                                                CurrentReportingPeriod :
                                                                                period == Consts.PreviousPeriod ?
                                                                                    PreviousReportingPeriod :
                                                                                    throw new Exception(Error.PeriodNotFound.GetMessage(period.ToString()));

    //YieldCurve
    public double[] GetYearlyYieldCurve(ImportIdentity id, string economicBasis)
    {
        var yc = GetYieldCurve(id, economicBasis);
        var ret = yc.Values.Skip(_args.Year - yc.Year);
        return (ret.Any() ? ret : yc.Values.Last().RepeatOnce()).ToArray();
    }

    // Modify this getter
    public YieldCurve GetYieldCurve(ImportIdentity id, string economicBasis) => (economicBasis, GetYieldCurvePeriod(id)) switch
    {
        (EconomicBases.C, PeriodType.BeginningOfPeriod) => GetShift(id.ProjectionPeriod) > 0
                                 ? GetCurrentYieldCurve(id.DataNode, Consts.CurrentPeriod)
                                 : GetCurrentYieldCurve(id.DataNode, Consts.PreviousPeriod),
        (EconomicBases.C, PeriodType.EndOfPeriod) => GetCurrentYieldCurve(id.DataNode, Consts.CurrentPeriod),
        (EconomicBases.L, _) => LockedInYieldCurve.TryGetValue(id.DataNode, out var yc) && yc != null ? yc :
                                throw new Exception(Error.YieldCurveNotFound.GetMessage(id.DataNode, DataNodeDataBySystemName[id.DataNode].ContractualCurrency,
                                                            DataNodeDataBySystemName[id.DataNode].Year.ToString(), _args.Month.ToString(),
                                                            _args.Scenario, DataNodeDataBySystemName[id.DataNode].YieldCurveName)),
        (_, PeriodType.NotApplicable) => throw new Exception(Error.YieldCurvePeriodNotApplicable.GetMessage(id.AocType, id.Novelty)),
        (_, _) => throw new Exception(Error.EconomicBasisNotFound.GetMessage(id.DataNode))
    };

    //Projection
    public int GetShift(int projectionPeriod) => ProjectionConfiguration[projectionPeriod].Shift;
    public int GetTimeStep(int projectionPeriod) => ProjectionConfiguration[projectionPeriod].TimeStep;
    public int GetProjectionCount(string dataNode) => ImportCalculationExtensions.GetProjections(GetRawVariables(dataNode), GetIfrsVariables(dataNode), ImportFormat, ProjectionConfiguration);


    public PeriodType GetPeriodType(string amountType, string estimateType)
    {
        if (estimateType == EstimateTypes.P) return PeriodType.EndOfPeriod;
        if (amountType != null && AmountTypeDimension.TryGetValue(amountType, out var at)) return at.PeriodType;
        if (estimateType != null && EstimateTypeDimension.TryGetValue(estimateType, out var ct)) return ct.PeriodType;
        return PeriodType.EndOfPeriod;
    }

    //Variables and Cash flows
    public IEnumerable<RawVariable> GetRawVariables(string dataNode) => RawVariablesByImportIdentity.TryGetValue(dataNode, out var variableCollection) ? variableCollection : Enumerable.Empty<RawVariable>();
    public IEnumerable<IfrsVariable> GetIfrsVariables(string dataNode) => IfrsVariablesByImportIdentity.TryGetValue(dataNode, out var variableCollection) ? variableCollection : Enumerable.Empty<IfrsVariable>();

    public double[] GetValues(ImportIdentity id, Func<RawVariable, bool> whereClause) => GetRawVariables(id.DataNode).Where(v => v.AocType == id.AocType && v.Novelty == id.Novelty && whereClause(v)).Select(v => v?.Values ?? null).AggregateDoubleArray();
    public double GetValue(ImportIdentity id, Func<IfrsVariable, bool> whereClause, int projection = 0) => GetIfrsVariables(id.DataNode).Where(v => v.AocType == id.AocType && v.Novelty == id.Novelty && whereClause(v)).Select(v => v?.Values ?? null).AggregateDoubleArray().ElementAtOrDefault(projection);

    public double[] GetValues(ImportIdentity id, string amountType, string estimateType, int? accidentYear) => GetValues(id, v => v.AccidentYear == accidentYear && v.AmountType == amountType && v.EstimateType == estimateType);
    public double GetValue(ImportIdentity id, string? amountType, string? estimateType, int? accidentYear,
        int projection = 0) => GetValue(id, v => v.AccidentYear == accidentYear && v.AmountType == amountType && v.EstimateType == estimateType, projection);
    public double GetValue(ImportIdentity id, string? amountType, string? estimateType, string? economicBasis,
        int? accidentYear, int projection = 0) => GetValue(id, v => v.AccidentYear == accidentYear && v.AmountType == amountType && v.EstimateType == estimateType && v.EconomicBasis == economicBasis, projection);

    //Novelty
    private IEnumerable<string> GetNoveltiesForAocType(string aocType, IEnumerable<AocStep> aocConfiguration) => aocConfiguration.Where(aocStep => aocStep.AocType == aocType).Select(aocStep => aocStep.Novelty);
    public IEnumerable<string> GetNovelties() => NoveltyDimension.Keys;
    public IEnumerable<string> GetNovelties(string aocType, StructureType structureTypes) => GetNoveltiesForAocType(aocType, aocStepByStructureType[structureTypes]);

    //Accident years
    public IEnumerable<int?> GetAccidentYears(string dataNode, int projectionPeriod)
    {
        if (!DataNodeDataBySystemName.TryGetValue(dataNode, out var dataNodeData)) 
            throw new Exception(Error.DataNodeNotFound.GetMessage(dataNode));

        if (AccidentYearsByDataNode.TryGetValue(dataNode, out var accidentYears))
            return dataNodeData.LiabilityType == LiabilityTypes.LIC
                ? accidentYears.Where(ay => Consts.MonthInAYear * ay <= Consts.MonthInAYear * _args.Year + GetShift(projectionPeriod) || ay == default).ToHashSet()
                : accidentYears;
        return new int?[] { default };
    }

    //Parameters
    public double GetNonPerformanceRiskRate(ImportIdentity identity, string cdrBasis)
    {
        if (!DataNodeDataBySystemName.TryGetValue(identity.DataNode, out var dataNodeData))
            throw new Exception(Error.DataNodeNotFound.GetMessage(identity.DataNode));
        if (dataNodeData.Partner == null) 
            throw new Exception(Error.PartnerNotFound.GetMessage(identity.DataNode));

        double rate;
        if (cdrBasis == EconomicBases.C)
        {
            var period = GetCreditDefaultRiskPeriod(identity) == PeriodType.BeginningOfPeriod ? Consts.PreviousPeriod : Consts.CurrentPeriod;
            // if Partner == Internal then return 0;
            if (!CurrentPartnerRating.TryGetValue(dataNodeData.Partner, out var currentRating))
                throw new Exception(Error.RatingNotFound.GetMessage(dataNodeData.Partner));
            if (!CurrentCreditDefaultRates.TryGetValue(currentRating[period].CreditRiskRating, out var currentRate))
                throw new Exception(Error.CreditDefaultRateNotFound.GetMessage(currentRating[period].CreditRiskRating));
            rate = currentRate[period].Values[0];
        }
        else
        {
            if (!LockedInPartnerRating[dataNodeData.Year].TryGetValue(dataNodeData.Partner, out var lockedRating)) 
                throw new Exception(Error.RatingNotFound.GetMessage(dataNodeData.Partner));
            if (!LockedInCreditDefaultRates[dataNodeData.Year].TryGetValue(lockedRating.CreditRiskRating, out var lockedRate))
                throw new Exception(Error.CreditDefaultRateNotFound.GetMessage(lockedRating.CreditRiskRating));
            rate = lockedRate.Values[0];
        }
        return Math.Pow(1d + rate, 1d / 12d) - 1d;
    }

    public (string, double[]) GetReleasePattern(ImportIdentity identity, string amountType, int patternShift)
    {
        var patternFromCashflow = GetValues(identity with { AocType = AocTypes.CL, Novelty = Novelties.C }, amountType, EstimateTypes.P, null);
        if (patternFromCashflow.Any())
            return (amountType, Enumerable.Repeat(0d, patternShift).Concat(patternFromCashflow).ToArray());

        if (SingleDataNodeParametersByGoc.TryGetValue(identity.DataNode, out var dataNodeParameterByPeriod) && dataNodeParameterByPeriod[Consts.CurrentPeriod].ReleasePattern != null)
        {
            var annualCohort = DataNodeDataBySystemName[identity.DataNode].AnnualCohort;
            var skipMonthsToCurrentReportingPeriod = Consts.MonthInAYear * (CurrentReportingPeriod.Year - annualCohort);
            var monthlyPattern = dataNodeParameterByPeriod[Consts.CurrentPeriod].ReleasePattern.Interpolate(dataNodeParameterByPeriod[Consts.CurrentPeriod].CashFlowPeriodicity, dataNodeParameterByPeriod[Consts.CurrentPeriod].InterpolationMethod);
            return (null, Enumerable.Repeat(0d, patternShift).Concat(monthlyPattern.Normalize()).Skip(skipMonthsToCurrentReportingPeriod).ToArray());
        }

        var patternFromCoverageUnits = GetValues(identity with { AocType = AocTypes.CL, Novelty = Novelties.C }, AmountTypes.CU, EstimateTypes.P, null);
        if (patternFromCoverageUnits.Any())
            return (AmountTypes.CU, Enumerable.Repeat(0d, patternShift).Concat(patternFromCoverageUnits).ToArray());

        if (DataNodeDataBySystemName[identity.DataNode].ValuationApproach == ValuationApproaches.PAA && DataNodeDataBySystemName[identity.DataNode].LiabilityType == LiabilityTypes.LRC)
            Warnings.Add(Warning.ReleasePatternNotFound.GetMessage(identity.DataNode, amountType));
        else
            throw new Exception(Error.ReleasePatternNotFound.GetMessage(identity.DataNode, amountType));

        return (null, Enumerable.Empty<double>().ToArray());
    }

    public double GetPremiumAllocationFactor(ImportIdentity id) =>
        SingleDataNodeParametersByGoc.TryGetValue(id.DataNode, out var singleDataNodeParameter)
            ? singleDataNodeParameter[Consts.CurrentPeriod].PremiumAllocation : Consts.DefaultPremiumExperienceAdjustmentFactor;

    public string GetEconomicBasisDriver(string dataNode) =>
        SingleDataNodeParametersByGoc.TryGetValue(dataNode, out var singleDataNodeParameter)
            ? singleDataNodeParameter[Consts.CurrentPeriod].EconomicBasisDriver
            : ImportCalculationExtensions.GetDefaultEconomicBasisDriver(DataNodeDataBySystemName[dataNode].ValuationApproach, DataNodeDataBySystemName[dataNode].LiabilityType);

    public bool IsInceptionYear(string dataNode) => SingleDataNodeParametersByGoc.TryGetValue(dataNode, out var singleDataNodeParameter)
            ? singleDataNodeParameter[Consts.CurrentPeriod].Year == CurrentReportingPeriod.Year : default;

    // IData Node relationships
    public IEnumerable<string> GetUnderlyingGic(ImportIdentity id) => !InterDataNodeParametersByGoc.TryGetValue(id.DataNode, out var interDataNodeParameters)
        ? Enumerable.Empty<string>()
        : interDataNodeParameters[Consts.CurrentPeriod].Select(x => x.DataNode != id.DataNode ? x.DataNode : x.LinkedDataNode).Where(goc => !DataNodeDataBySystemName[goc].IsReinsurance);

    public IEnumerable<string> GetUnderlyingGic(ImportIdentity id, string liabilityType) => GetUnderlyingGic(id).Where(goc => DataNodeDataBySystemName[goc].LiabilityType == liabilityType);

    public double GetReinsuranceCoverage(ImportIdentity id, string gic)
    {
        var targetPeriod = AocConfigurationByAocStep[new AocStep(id.AocType, id.Novelty)].RcPeriod == PeriodType.EndOfPeriod ? Consts.CurrentPeriod : Consts.PreviousPeriod;
        return InterDataNodeParametersByGoc.TryGetValue(id.DataNode, out var interDataNodeParameters)
            ? interDataNodeParameters[targetPeriod].FirstOrDefault(x => x.DataNode == gic || x.LinkedDataNode == gic).ReinsuranceCoverage
            : throw new Exception(Error.ReinsuranceCoverage.GetMessage(id.DataNode));
    }

    public ImportIdentity GetUnderlyingIdentity(ImportIdentity id, string gic)
    {
        if (!(DataNodeDataBySystemName.TryGetValue(id.DataNode, out var gricData) && gricData.IsReinsurance)) 
            throw new Exception(Error.InvalidGric.GetMessage(id.DataNode));
        if (!DataNodeDataBySystemName.TryGetValue(gic, out var gicData))
            throw new Exception(Error.InvalidGic.GetMessage(gic));

        return id with { DataNode = gic, ValuationApproach = gicData.ValuationApproach, IsReinsurance = gicData.IsReinsurance };
    }

    // Import Scope
    public bool IsPrimaryScope(string dataNode) => DataNodesByImportScope[ImportScope.Primary].Contains(dataNode);
    public bool IsSecondaryScope(string dataNode) => DataNodesByImportScope[ImportScope.Secondary].Contains(dataNode);

    // Other
    public IHierarchy<T> GetHierarchy<T>() where T : class, IHierarchicalDimension => _hierarchyCache.Get<T>();
    public IEnumerable<string> GetNonAttributableAmountType() => ImportCalculationExtensions.GetNonAttributableAmountTypes().SelectMany(at => _hierarchyCache.Get<AmountType>(at).Descendants(includeSelf: true).Select(x => x.SystemName));
    public IEnumerable<string> GetAttributableExpenseAndCommissionAmountType() => _hierarchyCache.Get<AmountType>(AmountTypes.ACA).Descendants(includeSelf: true).Select(x => x.SystemName)
                                                                                   .Concat(_hierarchyCache.Get<AmountType>(AmountTypes.AEA).Descendants(includeSelf: true).Select(x => x.SystemName));
    public IEnumerable<string> GetInvestmentClaims() => _hierarchyCache.Get<AmountType>(AmountTypes.ICO).Descendants(includeSelf: true).Select(x => x.SystemName);
    public IEnumerable<string> GetAttributableExpenses() => _hierarchyCache.Get<AmountType>(AmountTypes.AE).Descendants(includeSelf: true).Select(x => x.SystemName);
    public IEnumerable<string> GetDeferrableExpenses() => _hierarchyCache.Get<AmountType>(AmountTypes.DE).Descendants(includeSelf: true).Select(x => x.SystemName);
    public IEnumerable<string> GetPremiums() => _hierarchyCache.Get<AmountType>(AmountTypes.PR).Descendants(includeSelf: true).Select(x => x.SystemName);
    public IEnumerable<string> GetClaims() => _hierarchyCache.Get<AmountType>(AmountTypes.CL).Descendants().Select(x => x.SystemName);
    public IEnumerable<string> GetCdr() => _hierarchyCache.Get<AmountType>(AmountTypes.CDR).Descendants(includeSelf: true).Select(x => x.SystemName);
    public IEnumerable<string> GetCoverageUnits() => _hierarchyCache.Get<AmountType>(AmountTypes.CU).Descendants(includeSelf: true).Select(x => x.SystemName);

    private YieldCurve GetCurrentYieldCurve(string dn, int period) =>
        CurrentYieldCurve.TryGetValue(dn, out var ycByPeriod) && ycByPeriod != null && ycByPeriod.TryGetValue(period, out var yc) ? yc :
        throw new Exception(Error.YieldCurveNotFound.GetMessage(dn, DataNodeDataBySystemName[dn].ContractualCurrency, DataNodeDataBySystemName[dn].Year.ToString(),
                                                                _args.Month.ToString(), _args.Scenario, DataNodeDataBySystemName[dn].YieldCurveName));
    public void PopulateWorkspace(){}
}