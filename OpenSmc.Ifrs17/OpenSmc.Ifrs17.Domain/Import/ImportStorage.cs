//#!import "../Utils/EqualityComparers"
//#!import "../Utils/ImportCalculationMethods"
//#!import "../Utils/Queries"

using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Api;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Hierarchies;
using Systemorph.Vertex.Workspace;

public class ImportStorage
{   
    private readonly IDataSource querySource; 
    private readonly IWorkspace workspace;
    private readonly Systemorph.Vertex.Hierarchies.IHierarchicalDimensionCache hierarchyCache;
    private readonly ImportArgs args;
    
    //Format
    public string ImportFormat => args.ImportFormat; 
    
    //Time Periods 
    public (int Year, int Month) CurrentReportingPeriod => (args.Year, args.Month);
    public (int Year, int Month) PreviousReportingPeriod => (args.Year - 1, Consts.MonthInAYear); // YTD Logic

    //Partitions
    public Guid PartitionByRn;
    public Guid TargetPartition;
    public Guid DefaultPartition;
    public Guid PreviousPeriodPartition;    

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
    
    //Dimensions
    public Dictionary<string, AmountType> AmountTypeDimension { get; private set; }
    public Dictionary<string, Novelty> NoveltyDimension { get; private set; }
    public Dictionary<string, EstimateType> EstimateTypeDimension { get; private set; }
    public Dictionary<string, HashSet<string>> EstimateTypesByImportFormat { get; private set; }

    //Constructor
    public ImportStorage(ImportArgs args, IDataSource querySource, IWorkspace workspace)
    {
        this.querySource = querySource;
        this.workspace = workspace;
        hierarchyCache = workspace.ToHierarchicalDimensionCache();
        this.args = args;
    }
    
    //Initialize
    public async Task InitializeAsync()
    {   
        //Dimensions
        var estimateTypes = await workspace.Query<EstimateType>().ToArrayAsync();
        
        EstimateTypeDimension     = estimateTypes.ToDictionary(x => x.SystemName);
        AmountTypeDimension       = (await workspace.Query<AmountType>().ToArrayAsync()).ToDictionary(x => x.SystemName);
        NoveltyDimension          = (await workspace.Query<Novelty>().ToArrayAsync()).ToDictionary(x => x.SystemName);
                
        //Hierarchy Cache
        await hierarchyCache.InitializeAsync<AmountType>();
        
        //EstimateType to load and to update
        EstimateTypesByImportFormat = new InputSource[] { InputSource.Opening, InputSource.Actual, InputSource.Cashflow }
                                        .ToDictionary(x => x.ToString(), 
                                                      x => estimateTypes
                                                      .Where(et => et.InputSource.Contains(x))
                                                      .Select(et => et.SystemName)
                                                      .ToHashSet());
        
        //ProjectionConfiguration : Current Period + projection for every Quarter End for current Year and next Years as in projectionConfiguration.csv
        ProjectionConfiguration = (await workspace.Query<ProjectionConfiguration>().ToArrayAsync()).SortRelevantProjections(args.Month);
        FirstNextYearProjection = ProjectionConfiguration.TakeWhile(x => x.Shift <= CurrentReportingPeriod.Month).Count() + 
            (CurrentReportingPeriod.Month == Consts.MonthInAYear ? -1 : 0);
        
        //Get Partitions
        PartitionByRn = (await querySource.Query<PartitionByReportingNode>().Where(p => p.ReportingNode == args.ReportingNode).ToArrayAsync()).Single().Id;
        TargetPartition = (await querySource.Query<PartitionByReportingNodeAndPeriod>().Where(p => p.ReportingNode == args.ReportingNode &&
                                                                                                   p.Year == CurrentReportingPeriod.Year &&
                                                                                                   p.Month == CurrentReportingPeriod.Month &&
                                                                                                   p.Scenario == args.Scenario).ToArrayAsync()).Single().Id;
        DefaultPartition = (await querySource.Query<PartitionByReportingNodeAndPeriod>().Where(p => p.ReportingNode == args.ReportingNode &&
                                                                                                    p.Year == CurrentReportingPeriod.Year &&
                                                                                                    p.Month == CurrentReportingPeriod.Month &&
                                                                                                    p.Scenario == null).ToArrayAsync()).Single().Id;
        //Set Partitions
        await querySource.Partition.SetAsync<PartitionByReportingNode>(PartitionByRn);
        await workspace.Partition.SetAsync<PartitionByReportingNode>(PartitionByRn);
        
        await querySource.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(TargetPartition);
        await workspace.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(TargetPartition);
        
        //Get data from Workspace (result of parsing)
        var parsedRawVariables = await workspace.QueryPartitionedDataAsync<RawVariable,PartitionByReportingNodeAndPeriod>(querySource, TargetPartition, DefaultPartition, ImportFormat);
        var parsedIfrsVariables = await workspace.QueryPartitionedDataAsync<IfrsVariable,PartitionByReportingNodeAndPeriod>(querySource, TargetPartition, DefaultPartition, ImportFormat);
        
        //DataNodes
        DataNodeDataBySystemName = await workspace.LoadDataNodesAsync(args);
        
        //Accident Years
        AccidentYearsByDataNode = (IDictionary<string, ICollection<int?>>)
            (ImportFormat == ImportFormats.Cashflow ? parsedRawVariables.Select(x => new {x.DataNode, x.AccidentYear}) : parsedIfrsVariables.Select(x => new {x.DataNode, x.AccidentYear}))
            .ToDictionaryGrouped(x => x.DataNode, x => (ICollection<int?>)x.Select(y => y.AccidentYear).ToHashSet());
        
        // Import Scopes and Data Node relationship parameters
        InterDataNodeParametersByGoc = await workspace.LoadInterDataNodeParametersAsync(args);
        
        var primaryScopeFromParsedVariables = (ImportFormat == ImportFormats.Cashflow ? parsedRawVariables.Select(x => x.DataNode) : parsedIfrsVariables.Select(x => x.DataNode)).ToHashSet();
        var primaryScopeFromLinkedReinsurance = primaryScopeFromParsedVariables
                                            .Where(goc => !DataNodeDataBySystemName[goc].IsReinsurance && DataNodeDataBySystemName[goc].LiabilityType == LiabilityTypes.LRC)
                                            .SelectMany(goc => InterDataNodeParametersByGoc.TryGetValue(goc, out var interDataNodeParamByPeriod)
                                                                  ? interDataNodeParamByPeriod[Consts.CurrentPeriod].Select(param => param.DataNode == goc ? param.LinkedDataNode : param.DataNode).Where(goc => !primaryScopeFromParsedVariables.Contains(goc))
                                                                  : Enumerable.Empty<string>())
                                            .Except(primaryScopeFromParsedVariables).ToHashSet();
        var primaryScopeForPaaLrc = DataNodeDataBySystemName.Values.Where(dn => dn.LiabilityType == LiabilityTypes.LRC && dn.ValuationApproach == ValuationApproaches.PAA).Select(dn => dn.DataNode).Except(primaryScopeFromParsedVariables).ToHashSet();
        var addedToPrimaryScope = primaryScopeFromLinkedReinsurance.Concat(primaryScopeForPaaLrc).ToHashSet();
        var primaryScope = primaryScopeFromParsedVariables.Concat(primaryScopeFromLinkedReinsurance).Concat(primaryScopeForPaaLrc).ToHashSet();
        var secondaryScope = InterDataNodeParametersByGoc
                            .Where(kvp => primaryScope.Contains(kvp.Key))
                            .SelectMany(kvp => { var linkedGocs = kvp.Value[Consts.CurrentPeriod].Select(param => param.DataNode == kvp.Key ? param.LinkedDataNode : param.DataNode);
                                                return linkedGocs.Where(goc => !primaryScope.Contains(goc));}).ToHashSet();
        var allImportScopes = new HashSet<string>(primaryScope.Concat(secondaryScope));
        
        DataNodesByImportScope = new Dictionary<ImportScope, HashSet<string>> { { ImportScope.Primary, primaryScope }, { ImportScope.AddedToPrimary, addedToPrimaryScope }, { ImportScope.Secondary, secondaryScope } };
        
        // Parameters
        CurrentPartnerRating = await workspace.LoadCurrentAndPreviousParameterAsync<PartnerRating>(args, x => x.Partner);
        CurrentCreditDefaultRates = await workspace.LoadCurrentAndPreviousParameterAsync<CreditDefaultRate>(args, x => x.CreditRiskRating);
        var initialYears = DataNodeDataBySystemName.Values.Select(dn => dn.Year).ToHashSet();
        LockedInPartnerRating = new Dictionary<int, Dictionary<string, PartnerRating>>();
        LockedInCreditDefaultRates = new Dictionary<int, Dictionary<string, CreditDefaultRate>>();
        foreach (var year in initialYears)
        {
            LockedInPartnerRating[year] = await workspace.LoadCurrentParameterAsync<PartnerRating>(args with { Year = year, Month = args.Year == year ? args.Month : Consts.MonthInAYear }, x => x.Partner);
            LockedInCreditDefaultRates[year] = await workspace.LoadCurrentParameterAsync<CreditDefaultRate>(args with { Year = year, Month = args.Year == year ? args.Month : Consts.MonthInAYear }, x => x.CreditRiskRating);
        }
        SingleDataNodeParametersByGoc = await workspace.LoadSingleDataNodeParametersAsync(args);
        LockedInYieldCurve = await workspace.LoadLockedInYieldCurveAsync(args, allImportScopes.Select(dn => DataNodeDataBySystemName[dn]));
        CurrentYieldCurve = await workspace.LoadCurrentYieldCurveAsync(args, allImportScopes.Select(dn => DataNodeDataBySystemName[dn])); //TODO Rename this variable
        
        AocConfigurationByAocStep = await querySource.LoadAocStepConfigurationAsDictionaryAsync(args.Year, args.Month);
        aocStepByStructureType = ((StructureType[])Enum.GetValues(typeof(StructureType)))
            .ToDictionary(st => st, 
                          st => AocConfigurationByAocStep.Where(kvp => kvp.Value.StructureType.Contains(st)).Select(kvp => kvp.Key).ToHashSet());
        
        //Previous Period
        var openingRawVariables = Enumerable.Empty<RawVariable>();
        var openingIfrsVariables = Enumerable.Empty<IfrsVariable>();

        var allImportScopesAtInceptionYear = allImportScopes.Select(dn => DataNodeDataBySystemName[dn]).Where(dnd => dnd.Year == args.Year).Select(x => x.DataNode).ToHashSet();
        var allImportScopesNotAtInceptionYear = allImportScopes.Except(allImportScopesAtInceptionYear).ToHashSet();

        if(allImportScopesAtInceptionYear.Any()) {
            var dnAtInceptionYearFromParsedVar = allImportScopesAtInceptionYear.Where(dn => primaryScopeFromParsedVariables.Contains(dn));
            var dnAtInceptionYearNotFromParsedVar = allImportScopesAtInceptionYear.Except(dnAtInceptionYearFromParsedVar);

            openingIfrsVariables = await querySource.Query<IfrsVariable>()
                                    .Where(iv => iv.Partition == TargetPartition && iv.AocType == AocTypes.BOP && iv.Novelty == Novelties.I)
                                    .Where(iv => (dnAtInceptionYearFromParsedVar.Contains(iv.DataNode) && 
                                                 (ImportFormat != ImportFormats.Opening || !EstimateTypesByImportFormat[InputSource.Opening.ToString()].Contains(iv.EstimateType)) )
                                                 || dnAtInceptionYearNotFromParsedVar.Contains(iv.DataNode)).ToArrayAsync();
        }

        if(allImportScopesNotAtInceptionYear.Any()) {
            PreviousPeriodPartition = (await querySource.Query<PartitionByReportingNodeAndPeriod>()
                                        .Where(p => p.ReportingNode == args.ReportingNode && p.Year == PreviousReportingPeriod.Year 
                                                 && p.Month == PreviousReportingPeriod.Month && p.Scenario == null).ToArrayAsync()).SingleOrDefault()?.Id ?? 
                                      (Guid)ApplicationMessage.Log(Error.MissingPreviousPeriodData, PreviousReportingPeriod.Year.ToString(), PreviousReportingPeriod.Month.ToString(), string.Join(string.Empty, allImportScopesNotAtInceptionYear));
            
            await querySource.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(PreviousPeriodPartition);
        
            //Perform queries to previous Period
            openingRawVariables = (await querySource.Query<RawVariable>()
                                   .Where(rv => rv.Partition == PreviousPeriodPartition && rv.AocType == AocTypes.CL)
                                   .Where(v => primaryScope.Contains(v.DataNode)).ToArrayAsync())
                                   .Select(rv => rv with {AocType = AocTypes.BOP, Novelty = Novelties.I, 
                                                 Values = rv.Values.Skip(Consts.MonthInAYear).ToArray(), Partition = TargetPartition});
            
            openingIfrsVariables = openingIfrsVariables.Union((await querySource.Query<IfrsVariable>()
                                    .Where(iv => iv.Partition == PreviousPeriodPartition && iv.AocType == AocTypes.EOP)
                                    .Where(v => allImportScopesNotAtInceptionYear.Contains(v.DataNode)).ToArrayAsync())
                                    .Select(iv => iv with {AocType = AocTypes.BOP, Novelty = Novelties.I, Partition = TargetPartition}),
                                    OpenSmc.Ifrs17.Domain.Utils.EqualityComparer<IfrsVariable>.Instance);
            
            await querySource.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(TargetPartition);

            // TODO: print error if 
            //openingRawVariables.Select(x => x.DataNode).ToHashSet() != dataNodesWithPreviousPeriod
        }
        
        //Variables
        var rawVariables = parsedRawVariables.Concat(openingRawVariables)
            .Concat(await querySource.Query<RawVariable>().Where(rv => rv.Partition == TargetPartition)
            .Where(rv => addedToPrimaryScope.Contains(rv.DataNode)).ToArrayAsync());
        
        var ifrsVariables = parsedIfrsVariables.Union(openingIfrsVariables, OpenSmc.Ifrs17.Domain.Utils.EqualityComparer<IfrsVariable>.Instance)
            .Union(await querySource.Query<IfrsVariable>().Where(iv => iv.Partition == TargetPartition && !(iv.AocType == AocTypes.BOP && iv.Novelty == Novelties.I) && 
            primaryScope.Contains(iv.DataNode) || secondaryScope.Contains(iv.DataNode)).ToArrayAsync(), OpenSmc.Ifrs17.Domain.Utils.EqualityComparer<IfrsVariable>.Instance);                                                                       

        if(DefaultPartition != TargetPartition) {
            await querySource.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(DefaultPartition);
            var defaultRawVariables = await querySource.Query<RawVariable>().Where(rv => rv.Partition == DefaultPartition && primaryScope.Contains(rv.DataNode)).ToArrayAsync();
            var defaultIfrsVariables = await querySource.Query<IfrsVariable>().Where(iv => iv.Partition == DefaultPartition && allImportScopes.Contains(iv.DataNode)).ToArrayAsync();         
            rawVariables = rawVariables.Union(defaultRawVariables, OpenSmc.Ifrs17.Domain.Utils.EqualityComparer<RawVariable>.Instance);
            ifrsVariables = ifrsVariables.Union(defaultIfrsVariables, OpenSmc.Ifrs17.Domain.Utils.EqualityComparer<IfrsVariable>.Instance);
            await querySource.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(TargetPartition);
        }

        RawVariablesByImportIdentity = (IDictionary<string, ICollection<RawVariable>>)rawVariables.ToDictionaryGrouped(v => v.DataNode, v => (ICollection<RawVariable>)v.ToArray());
        IfrsVariablesByImportIdentity = (IDictionary<string, ICollection<IfrsVariable>>)ifrsVariables.ToDictionaryGrouped(v => v.DataNode, v => (ICollection<IfrsVariable>)v.ToArray());
    }
    
    //Getters
    
    //Periods
    public ValuationPeriod GetValuationPeriod(ImportIdentity id) => AocConfigurationByAocStep[new AocStep(id.AocType, id.Novelty)].ValuationPeriod;
    public PeriodType GetYieldCurvePeriod(ImportIdentity id) => AocConfigurationByAocStep[new AocStep(id.AocType, id.Novelty)].YcPeriod;
    public PeriodType GetCreditDefaultRiskPeriod(ImportIdentity id) => AocConfigurationByAocStep[new AocStep(id.AocType, id.Novelty)].CdrPeriod;
    
    public IEnumerable<AocStep> GetAllAocSteps(StructureType structureType) => aocStepByStructureType[structureType];
    
    public IEnumerable<AocStep> GetCalculatedTelescopicAocSteps() => AocConfigurationByAocStep.Where(kvp => kvp.Value.DataType == DataType.CalculatedTelescopic).Select(kvp => kvp.Key);

    public DataNodeData GetDataNodeData(ImportIdentity id) => DataNodeDataBySystemName[id.DataNode];

    public SingleDataNodeParameter GetSingleDataNodeParameter(ImportIdentity id, int period) => SingleDataNodeParametersByGoc.TryGetValue(id.DataNode, out var singleDataNodeParameter)
            ? singleDataNodeParameter[period] : (SingleDataNodeParameter)ApplicationMessage.Log(Error.MissingSingleDataNodeParameter, id.DataNode);
    
    public (int Year, int Month) GetReportingPeriod(int period) => period == Consts.CurrentPeriod ?
                                                                                CurrentReportingPeriod :
                                                                                period == Consts.PreviousPeriod ? 
                                                                                    PreviousReportingPeriod :
                                                                                    ((int, int))ApplicationMessage.Log(Error.PeriodNotFound, period.ToString());
    
    //YieldCurve
    public double[] GetYearlyYieldCurve(ImportIdentity id, string economicBasis) {
        var yc = GetYieldCurve(id, economicBasis);
        var ret = yc.Values.Skip(args.Year - yc.Year);
        return (ret.Any() ? ret : yc.Values.Last().RepeatOnce()).ToArray();
    }
    
    // Modify this getter
    public YieldCurve GetYieldCurve(ImportIdentity id, string economicBasis) => (economicBasis, GetYieldCurvePeriod(id)) switch {
            (EconomicBases.C, PeriodType.BeginningOfPeriod ) => GetShift(id.ProjectionPeriod) > 0 
                                     ? GetCurrentYieldCurve(id.DataNode, Consts.CurrentPeriod) 
                                     : GetCurrentYieldCurve(id.DataNode, Consts.PreviousPeriod),
            (EconomicBases.C, PeriodType.EndOfPeriod) => GetCurrentYieldCurve(id.DataNode, Consts.CurrentPeriod),            
            (EconomicBases.L, _ ) => LockedInYieldCurve.TryGetValue(id.DataNode, out var yc) && yc != null ? yc : 
                                    (YieldCurve)ApplicationMessage.Log(Error.YieldCurveNotFound, id.DataNode, DataNodeDataBySystemName[id.DataNode].ContractualCurrency, 
                                                                DataNodeDataBySystemName[id.DataNode].Year.ToString(), args.Month.ToString(), 
                                                                args.Scenario, DataNodeDataBySystemName[id.DataNode].YieldCurveName),
            (_, PeriodType.NotApplicable) => (YieldCurve)ApplicationMessage.Log(Error.YieldCurvePeriodNotApplicable, id.AocType, id.Novelty),
            (_, _) => (YieldCurve)ApplicationMessage.Log(Error.EconomicBasisNotFound, id.DataNode)
       };

    //Projection
    public int GetShift(int projectionPeriod) => ProjectionConfiguration[projectionPeriod].Shift;
    public int GetTimeStep(int projectionPeriod) => ProjectionConfiguration[projectionPeriod].TimeStep;
    public int GetProjectionCount(string dataNode) => ImportCalculationExtensions.GetProjections(GetRawVariables(dataNode), GetIfrsVariables(dataNode), ImportFormat, ProjectionConfiguration);


    public PeriodType GetPeriodType(string amountType, string estimateType) 
    {
        if (estimateType == EstimateTypes.P)                                                         return PeriodType.EndOfPeriod;
        if (amountType != null && AmountTypeDimension.TryGetValue(amountType, out var at))           return at.PeriodType;
        if (estimateType != null && EstimateTypeDimension.TryGetValue(estimateType, out var ct))     return ct.PeriodType;
        return PeriodType.EndOfPeriod;
    }

    //Variables and Cash flows
    public IEnumerable<RawVariable> GetRawVariables(string dataNode) => RawVariablesByImportIdentity.TryGetValue(dataNode, out var variableCollection) ? variableCollection : Enumerable.Empty<RawVariable>();
    public IEnumerable<IfrsVariable> GetIfrsVariables(string dataNode) => IfrsVariablesByImportIdentity.TryGetValue(dataNode, out var variableCollection) ? variableCollection : Enumerable.Empty<IfrsVariable>();
    
    public double[] GetValues(ImportIdentity id, Func<RawVariable, bool> whereClause) => GetRawVariables(id.DataNode).Where(v => v.AocType == id.AocType && v.Novelty == id.Novelty && whereClause(v)).Select(v => v?.Values ?? (double[])null).AggregateDoubleArray();
    public double GetValue(ImportIdentity id, Func<IfrsVariable, bool> whereClause, int projection = 0) => GetIfrsVariables(id.DataNode).Where(v => v.AocType == id.AocType && v.Novelty == id.Novelty && whereClause(v)).Select(v => v?.Values ?? (double[])null).AggregateDoubleArray().ElementAtOrDefault(projection);
    
    public double[] GetValues(ImportIdentity id, string amountType, string estimateType, int? accidentYear) => GetValues(id, v => v.AccidentYear == accidentYear && v.AmountType == amountType && v.EstimateType == estimateType);
    public double GetValue(ImportIdentity id, string amountType, string estimateType, int? accidentYear, int projection = 0) => GetValue(id, v => v.AccidentYear == accidentYear && v.AmountType == amountType && v.EstimateType == estimateType, projection);
    public double GetValue(ImportIdentity id, string amountType, string estimateType, string economicBasis, int? accidentYear, int projection = 0) => GetValue(id, v => v.AccidentYear == accidentYear && v.AmountType == amountType && v.EstimateType == estimateType && v.EconomicBasis == economicBasis, projection);
   
    //Novelty
    private IEnumerable<string> GetNoveltiesForAocType(string aocType, IEnumerable<AocStep> aocConfiguration) => aocConfiguration.Where(aocStep => aocStep.AocType == aocType).Select(aocStep => aocStep.Novelty);
    public IEnumerable<string> GetNovelties() => NoveltyDimension.Keys;
    public IEnumerable<string> GetNovelties(string aocType, StructureType structureType) => GetNoveltiesForAocType(aocType, aocStepByStructureType[structureType]);
    
    //Accident years
    public IEnumerable<int?> GetAccidentYears(string dataNode, int projectionPeriod) { 
        if(!DataNodeDataBySystemName.TryGetValue(dataNode, out var dataNodeData))       ApplicationMessage.Log(Error.DataNodeNotFound, dataNode);
        if (AccidentYearsByDataNode.TryGetValue(dataNode, out var accidentYears))
            return dataNodeData.LiabilityType == LiabilityTypes.LIC
                ? accidentYears.Where(ay => Consts.MonthInAYear * ay <= (Consts.MonthInAYear * args.Year + GetShift(projectionPeriod)) || ay == default).ToHashSet()
                : accidentYears;
        return new int?[] { default };
        }
    
    //Parameters
    public double GetNonPerformanceRiskRate (ImportIdentity identity, string cdrBasis)
    {
        if(!DataNodeDataBySystemName.TryGetValue(identity.DataNode, out var dataNodeData))      ApplicationMessage.Log(Error.DataNodeNotFound, identity.DataNode);
        if(dataNodeData.Partner == null)                                                        ApplicationMessage.Log(Error.PartnerNotFound, identity.DataNode);
        
        double rate;
        if(cdrBasis == EconomicBases.C)
        { 
            var period = GetCreditDefaultRiskPeriod(identity) == PeriodType.BeginningOfPeriod ? Consts.PreviousPeriod : Consts.CurrentPeriod;
            // if Partner == Internal then return 0;
            if(!CurrentPartnerRating.TryGetValue(dataNodeData.Partner, out var currentRating))                          ApplicationMessage.Log(Error.RatingNotFound, dataNodeData.Partner);
            if(!CurrentCreditDefaultRates.TryGetValue(currentRating[period].CreditRiskRating, out var currentRate))     ApplicationMessage.Log(Error.CreditDefaultRateNotFound, currentRating[period].CreditRiskRating);
            rate = currentRate[period].Values[0];
        }
        else
        {
            if(!LockedInPartnerRating[dataNodeData.Year].TryGetValue(dataNodeData.Partner, out var lockedRating))               ApplicationMessage.Log(Error.RatingNotFound, dataNodeData.Partner);
            if(!LockedInCreditDefaultRates[dataNodeData.Year].TryGetValue(lockedRating.CreditRiskRating, out var lockedRate))   ApplicationMessage.Log(Error.CreditDefaultRateNotFound, lockedRating.CreditRiskRating);
            rate = lockedRate.Values[0];
        }
        return Math.Pow(1d + rate, 1d / 12d) - 1d;
    }
    
    public (string, double[]) GetReleasePattern (ImportIdentity identity, string amountType, int patternShift)
    {
        var patternFromCashflow = GetValues(identity with {AocType = AocTypes.CL, Novelty = Novelties.C}, amountType, EstimateTypes.P, (int?)null);
        if (patternFromCashflow.Any())
            return (amountType, Enumerable.Repeat(0d, patternShift).Concat(patternFromCashflow).ToArray());
        
        if(SingleDataNodeParametersByGoc.TryGetValue(identity.DataNode, out var dataNodeParameterByPeriod) && dataNodeParameterByPeriod[Consts.CurrentPeriod].ReleasePattern != null){
            var annualCohort = DataNodeDataBySystemName[identity.DataNode].AnnualCohort;
            var skipMonthsToCurrentReportingPeriod = Consts.MonthInAYear * (CurrentReportingPeriod.Year - annualCohort);
            var monthlyPattern = dataNodeParameterByPeriod[Consts.CurrentPeriod].ReleasePattern.Interpolate(dataNodeParameterByPeriod[Consts.CurrentPeriod].CashFlowPeriodicity, dataNodeParameterByPeriod[Consts.CurrentPeriod].InterpolationMethod);
            return (null, Enumerable.Repeat(0d, patternShift).Concat(monthlyPattern.Normalize()).Skip(skipMonthsToCurrentReportingPeriod).ToArray());
        }

        var patternFromCoverageUnits = GetValues(identity with {AocType = AocTypes.CL, Novelty = Novelties.C}, AmountTypes.CU, EstimateTypes.P, (int?)null);
        if (patternFromCoverageUnits.Any())
            return (AmountTypes.CU, Enumerable.Repeat(0d, patternShift).Concat(patternFromCoverageUnits).ToArray());

        if(DataNodeDataBySystemName[identity.DataNode].ValuationApproach == ValuationApproaches.PAA && DataNodeDataBySystemName[identity.DataNode].LiabilityType == LiabilityTypes.LRC)
            ApplicationMessage.Log(Warning.ReleasePatternNotFound, identity.DataNode, amountType);
        else ApplicationMessage.Log(Error.ReleasePatternNotFound, identity.DataNode, amountType);
        
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
    
    // Data Node relationships
    public IEnumerable<string> GetUnderlyingGic(ImportIdentity id) => !InterDataNodeParametersByGoc.TryGetValue(id.DataNode, out var interDataNodeParameters)
        ? Enumerable.Empty<string>()
        : interDataNodeParameters[Consts.CurrentPeriod].Select(x => x.DataNode != id.DataNode ? x.DataNode : x.LinkedDataNode).Where(goc => !DataNodeDataBySystemName[goc].IsReinsurance);

    public IEnumerable<string> GetUnderlyingGic(ImportIdentity id, string liabilityType) => GetUnderlyingGic(id).Where(goc => DataNodeDataBySystemName[goc].LiabilityType == liabilityType);
    
    public double GetReinsuranceCoverage (ImportIdentity id, string gic)  
    {
        var targetPeriod = AocConfigurationByAocStep[new AocStep(id.AocType, id.Novelty)].RcPeriod == PeriodType.EndOfPeriod ? Consts.CurrentPeriod : Consts.PreviousPeriod;
        return InterDataNodeParametersByGoc.TryGetValue(id.DataNode, out var interDataNodeParameters)
            ? interDataNodeParameters[targetPeriod].FirstOrDefault(x => x.DataNode == gic || x.LinkedDataNode == gic).ReinsuranceCoverage
            : (double)ApplicationMessage.Log(Error.ReinsuranceCoverage, id.DataNode);
    }

    public ImportIdentity GetUnderlyingIdentity(ImportIdentity id, string gic) {
        if(!(DataNodeDataBySystemName.TryGetValue(id.DataNode, out var gricData) && gricData.IsReinsurance)) ApplicationMessage.Log(Error.InvalidGric, id.DataNode);
        if(!DataNodeDataBySystemName.TryGetValue(gic, out var gicData))                                      ApplicationMessage.Log(Error.InvalidGic, gic);
        
        return id with {DataNode = gic, ValuationApproach = gicData.ValuationApproach, IsReinsurance = gicData.IsReinsurance};
    }
    
    // Import Scope
    public bool IsPrimaryScope (string dataNode) => DataNodesByImportScope[ImportScope.Primary].Contains(dataNode);
    public bool IsSecondaryScope (string dataNode) => DataNodesByImportScope[ImportScope.Secondary].Contains(dataNode);
    
    // Other
    public Systemorph.Vertex.Hierarchies.IHierarchy<T> GetHierarchy<T>() where T : class, IHierarchicalDimension => hierarchyCache.Get<T>();
    public IEnumerable<string> GetNonAttributableAmountType() => ImportCalculationExtensions.GetNonAttributableAmountTypes().SelectMany(at => hierarchyCache.Get<AmountType>(at).Descendants(includeSelf : true).Select(x => x.SystemName));
    public IEnumerable<string> GetAttributableExpenseAndCommissionAmountType() => hierarchyCache.Get<AmountType>(AmountTypes.ACA).Descendants(includeSelf : true).Select(x => x.SystemName)
                                                                                   .Concat(hierarchyCache.Get<AmountType>(AmountTypes.AEA).Descendants(includeSelf : true).Select(x => x.SystemName));
    public IEnumerable<string> GetInvestmentClaims() => hierarchyCache.Get<AmountType>(AmountTypes.ICO).Descendants(includeSelf : true).Select(x => x.SystemName);
    public IEnumerable<string> GetAttributableExpenses() => hierarchyCache.Get<AmountType>(AmountTypes.AE).Descendants(includeSelf : true).Select(x => x.SystemName);
    public IEnumerable<string> GetDeferrableExpenses() => hierarchyCache.Get<AmountType>(AmountTypes.DE).Descendants(includeSelf : true).Select(x => x.SystemName);
    public IEnumerable<string> GetPremiums() => hierarchyCache.Get<AmountType>(AmountTypes.PR).Descendants(includeSelf : true).Select(x => x.SystemName);
    public IEnumerable<string> GetClaims() => hierarchyCache.Get<AmountType>(AmountTypes.CL).Descendants().Select(x => x.SystemName);
    public IEnumerable<string> GetCdr() => hierarchyCache.Get<AmountType>(AmountTypes.CDR).Descendants(includeSelf: true).Select(x => x.SystemName);
    public IEnumerable<string> GetCoverageUnits() => hierarchyCache.Get<AmountType>(AmountTypes.CU).Descendants(includeSelf: true).Select(x => x.SystemName);

    private YieldCurve GetCurrentYieldCurve(string dn, int period) => 
        CurrentYieldCurve.TryGetValue(dn, out var ycByPeriod) && ycByPeriod != null  && ycByPeriod.TryGetValue(period, out var yc) ? yc : 
        (YieldCurve)ApplicationMessage.Log(Error.YieldCurveNotFound, dn , DataNodeDataBySystemName[dn].ContractualCurrency, DataNodeDataBySystemName[dn].Year.ToString(), 
                                        args.Month.ToString(), args.Scenario, DataNodeDataBySystemName[dn].YieldCurveName);
}



