using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.Constants.Validations;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.Args;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Hierarchies;
using Systemorph.Vertex.Workspace;

namespace OpenSmc.Ifrs17.Domain.Import;

public class ParsingStorage
{
    private readonly IDataSource dataSource;
    private readonly IWorkspace workspace;
    private readonly ImportArgs args;
    
    //Hierarchy Cache
    public IHierarchicalDimensionCache HierarchyCache;
    
    public ReportingNode ReportingNode { get; protected set; }
    
    public Dictionary<string, DataNodeData> DataNodeDataBySystemName;
    
    // Dimensions
    public Dictionary<string, EstimateType> EstimateType;
    public Dictionary<string, AmountType> AmountType; 
    public HashSet<AocStep> MandatoryAocSteps;
    public HashSet<AocStep> AocTypeMap;
    private HashSet<string> estimateTypes;
    private HashSet<string> amountTypes;
    private HashSet<string> economicBasis;
    private Dictionary<string, HashSet<string>> amountTypesByEstimateType => ImportCalculationExtensions.GetAmountTypesByEstimateType(HierarchyCache);
    public HashSet<string> TechnicalMarginEstimateTypes => ImportCalculationExtensions.GetTechnicalMarginEstimateType(); 
    public Dictionary<Type, Dictionary<string, string>> DimensionsWithExternalId;
    public Dictionary<string, Dictionary<int, SingleDataNodeParameter>> SingleDataNodeParametersByGoc { get; private set; }

    // Partitions
    public PartitionByReportingNode TargetPartitionByReportingNode;
    public PartitionByReportingNodeAndPeriod TargetPartitionByReportingNodeAndPeriod;
    
    //Constructor
    public ParsingStorage(ImportArgs args, IDataSource dataSource, IWorkspace workspace)
    {
        this.args = args;
        this.dataSource = dataSource;
        this.workspace = workspace;
    }
    
    // Initialize
    public async Task InitializeAsync()
    {
        //Partition Workspace and DataSource
        TargetPartitionByReportingNode = (await workspace.Query<PartitionByReportingNode>().Where(p => p.ReportingNode == args.ReportingNode).ToArrayAsync()).SingleOrDefault(); 
        
        if(TargetPartitionByReportingNode == null) 
        { ApplicationMessage.Log(Error.ParsedPartitionNotFound, args.ReportingNode); return; } 
        
        await workspace.Partition.SetAsync<PartitionByReportingNode>(TargetPartitionByReportingNode.Id);
        await dataSource.Partition.SetAsync<PartitionByReportingNode>(TargetPartitionByReportingNode.Id);
        
        if(args.ImportFormat == ImportFormats.Cashflow || args.ImportFormat == ImportFormats.Actual || 
           args.ImportFormat == ImportFormats.SimpleValue || args.ImportFormat == ImportFormats.Opening)
        {
            TargetPartitionByReportingNodeAndPeriod = (await workspace.Query<PartitionByReportingNodeAndPeriod>()
                .Where(p => p.ReportingNode == args.ReportingNode &&
                            p.Year == args.Year &&
                            p.Month == args.Month &&
                            p.Scenario == args.Scenario).ToArrayAsync()).SingleOrDefault();
            
            if(TargetPartitionByReportingNodeAndPeriod == null) 
            { ApplicationMessage.Log(Error.ParsedPartitionNotFound, args.ReportingNode, args.Year.ToString(), args.Month.ToString(), args.Scenario); return; } 
        
            await workspace.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(TargetPartitionByReportingNodeAndPeriod.Id);
            await dataSource.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(TargetPartitionByReportingNodeAndPeriod.Id);
            
            //Clean up the workspace
            await workspace.DeleteAsync<RawVariable>( await workspace.Query<RawVariable>().ToArrayAsync() );
            await workspace.DeleteAsync<IfrsVariable>( await workspace.Query<IfrsVariable>().ToArrayAsync() );
        }
        
        var reportingNodes = (await dataSource.Query<ReportingNode>().Where(x => x.SystemName == args.ReportingNode).ToArrayAsync());
        if(!reportingNodes.Any()) { ApplicationMessage.Log(Error.ReportingNodeNotFound, args.ReportingNode); return; }
        ReportingNode = reportingNodes.First();

        var aocConfigurationByAocStep = await dataSource.LoadAocStepConfigurationAsync(args.Year, args.Month);
        MandatoryAocSteps = aocConfigurationByAocStep.Where(x => x.DataType == DataType.Mandatory).Select(x => new AocStep(x.AocType, x.Novelty)).ToHashSet();
        AocTypeMap = args.ImportFormat switch {
            ImportFormats.Cashflow => aocConfigurationByAocStep.Where(x => x.InputSource.Contains(InputSource.Cashflow) &&
                                                                           !new DataType[]{DataType.Calculated, DataType.CalculatedTelescopic}.Contains(x.DataType) )
                .GroupBy(x => new AocStep(x.AocType, x.Novelty), (k,v) => k).ToHashSet(),
            ImportFormats.Actual => aocConfigurationByAocStep.Where(x => x.InputSource.Contains(InputSource.Actual) &&
                                                                         !new DataType[]{DataType.Calculated, DataType.CalculatedTelescopic}.Contains(x.DataType) && 
                                                                         new AocStep(x.AocType, x.Novelty) != new AocStep(AocTypes.BOP, Novelties.I))
                .GroupBy(x => new AocStep(x.AocType, x.Novelty), (k,v) => k).ToHashSet(),
            ImportFormats.Opening => aocConfigurationByAocStep.Where(x => x.InputSource.Contains(InputSource.Opening) && x.DataType == DataType.Optional).GroupBy(x => new AocStep(x.AocType, x.Novelty), (k,v) => k).ToHashSet(),
            ImportFormats.SimpleValue => aocConfigurationByAocStep.GroupBy(x => new AocStep(x.AocType, x.Novelty), (k,v) => k).Concat((await dataSource.Query<PnlVariableType>().ToArrayAsync())
                .Select(vt => new AocStep(vt.SystemName,null))).ToHashSet(),
            _ => Enumerable.Empty<AocStep>().ToHashSet(),
        };
        DataNodeDataBySystemName = args.ImportFormat == ImportFormats.Opening 
            ? (await dataSource.LoadDataNodesAsync(args)).Where(kvp => kvp.Value.Year == args.Year).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            : await dataSource.LoadDataNodesAsync(args);

        SingleDataNodeParametersByGoc = await dataSource.LoadSingleDataNodeParametersAsync(args);

        // Dimensions
        EstimateType = (await dataSource.Query<EstimateType>().ToArrayAsync()).ToDictionary(x => x.SystemName);
        AmountType = (await dataSource.Query<AmountType>().Where(x =>!(x is DeferrableAmountType)).ToArrayAsync()).ToDictionary(x => x.SystemName);
        amountTypes = (await dataSource.Query<AmountType>().ToArrayAsync()).Select(at => at.SystemName).ToHashSet();
        economicBasis = (await dataSource.Query<EconomicBasis>().ToArrayAsync()).Select(eb => eb.SystemName).ToHashSet();
        estimateTypes = args.ImportFormat switch {
            ImportFormats.SimpleValue => (await dataSource.Query<EstimateType>().ToArrayAsync()).Select(et => et.SystemName).ToHashSet(),
            ImportFormats.Opening => (await dataSource.Query<EstimateType>().Where(et => et.StructureType != StructureType.None).ToArrayAsync())
                .Where(et => et.InputSource.Contains(InputSource.Opening)) //This Contains overload cannot be used in DB, thus the ToArrayAsync()
                .Select(et => et.SystemName).ToHashSet(),
            _ => Enumerable.Empty<string>().ToHashSet(),
        };
        
        
        // DimensionsWithExternalId
        DimensionsWithExternalId = new Dictionary<Type, Dictionary<string, string>>()
        {
            { typeof(AmountType), await GetDimensionWithExternalIdDictionaryAsync<AmountType>() },
            { typeof(EstimateType), await GetDimensionWithExternalIdDictionaryAsync<EstimateType>() }
        };
        
        //Hierarchy Cache
        HierarchyCache = workspace.ToHierarchicalDimensionCache();
        await HierarchyCache.InitializeAsync<AmountType>();
    }
    
    public async Task<Dictionary<string, string>> GetDimensionWithExternalIdDictionaryAsync<T> () where T : KeyedOrderedDimension
    {
        var dict = new Dictionary<string, string>();
        var items = await dataSource.Query<T>().ToArrayAsync();
        foreach (var item in items) {
            dict.TryAdd(item.SystemName, item.SystemName);
            if(typeof(T).IsAssignableTo(typeof(KeyedOrderedDimensionWithExternalId))) {
                var externalIds = (string[])(typeof(T).GetProperty(nameof(KeyedOrderedDimensionWithExternalId.ExternalId)).GetValue(item));
                if(externalIds == null) continue;
                foreach (var extId in externalIds) 
                    dict.TryAdd(extId, item.SystemName);
            }
        }
        return dict;
    }
    
    // Getters
    public bool IsDataNodeReinsurance(string goc) => DataNodeDataBySystemName[goc].IsReinsurance;
    public bool IsValidDataNode(string goc) => DataNodeDataBySystemName.ContainsKey(goc);

    public CashFlowPeriodicity GetCashFlowPeriodicity(string goc) {
        if(!SingleDataNodeParametersByGoc.TryGetValue(goc, out var inner)) 
            return CashFlowPeriodicity.Monthly;
        return inner[Consts.CurrentPeriod].CashFlowPeriodicity; 
    }

    public InterpolationMethod GetInterpolationMethod(string goc) {
        if(!SingleDataNodeParametersByGoc.TryGetValue(goc, out var inner))
            return InterpolationMethod.NotApplicable;
        return inner[Consts.CurrentPeriod].InterpolationMethod; 
    }

    // Validations
    public string ValidateEstimateType(string et, string goc) {
        var allowedEstimateTypes = estimateTypes;
        if (DataNodeDataBySystemName.TryGetValue(goc, out var dataNodeData) && dataNodeData.LiabilityType == LiabilityTypes.LIC)
            estimateTypes.ExceptWith(TechnicalMarginEstimateTypes);
        if(!allowedEstimateTypes.Contains(et))
            ApplicationMessage.Log(Error.EstimateTypeNotFound, et);
        return et;
    }
    
    public string ValidateAmountType(string at) {
        if (at != null && !amountTypes.Contains(at))
            ApplicationMessage.Log(Error.AmountTypeNotFound, at);
        return at;
    }
    
    public AocStep ValidateAocStep(AocStep aoc) {
        if (!AocTypeMap.Contains(aoc))
            ApplicationMessage.Log(Error.AocTypeMapNotFound, aoc.AocType, aoc.Novelty);
        return aoc;
    }
    
    public string ValidateDataNode(string goc, string importFormat) {
        if (!DataNodeDataBySystemName.ContainsKey(goc))
        {
            if( importFormat == ImportFormats.Opening )
                ApplicationMessage.Log(Error.InvalidDataNodeForOpening, goc);
            else
                ApplicationMessage.Log(Error.InvalidDataNode, goc);
        }
        return goc;
    }
    
    public void ValidateEstimateTypeAndAmountType(string estimateType, string amountType){
        if (amountTypesByEstimateType.TryGetValue(estimateType, out var ats) && ats.Any() && !ats.Contains(amountType))
            ApplicationMessage.Log(Error.InvalidAmountTypeEstimateType, estimateType, amountType);
    }

    public string ValidateEconomicBasisDriver(string eb, string goc){
        if(string.IsNullOrEmpty(eb))
            return ImportCalculationExtensions.GetDefaultEconomicBasisDriver(DataNodeDataBySystemName[goc].ValuationApproach, DataNodeDataBySystemName[goc].LiabilityType);
        if(!economicBasis.Contains(eb)){
            ApplicationMessage.Log(Error.InvalidEconomicBasisDriver, goc);
            return null;
        }
        return eb;
    }
}