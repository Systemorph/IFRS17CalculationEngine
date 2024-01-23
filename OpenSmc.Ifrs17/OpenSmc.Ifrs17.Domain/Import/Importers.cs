using System.Diagnostics;
using System.Linq.Expressions;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.DataStructures;
using Systemorph.Vertex.Hierarchies;
using Systemorph.Vertex.Workspace;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Import;
using Systemorph.Scopes;
using Systemorph.Vertex.Arithmetics;
using Systemorph.Vertex.Scopes.Proxy;
using Debug = OpenSmc.Ifrs17.Domain.Constants.Debug;
using Scenario = OpenSmc.Ifrs17.Domain.DataModel.Scenario;
using Scenarios = OpenSmc.Ifrs17.Domain.Constants.Scenarios;

namespace OpenSmc.Ifrs17.Domain.Import;

public static class ImpoterUtils
{
    public static int? ParseAccidentYear(this IDataSet dataSet, IDataRow dataRow, string tableName) =>
        dataSet.Tables[tableName].Columns.Any(x => x.ColumnName == nameof(RawVariable.AccidentYear)) &&
        !string.IsNullOrEmpty(dataRow.Field<string>(nameof(RawVariable.AccidentYear)))
            ? Int32.TryParse(dataRow.Field<string>(nameof(RawVariable.AccidentYear)), out var accidentYear)
                ? accidentYear
                : (int?) ApplicationMessage.Log(Error.AccidentYearTypeNotValid,
                    dataRow.Field<string>(nameof(RawVariable.AccidentYear)))
            : null;


    public static TEnum ParseEnumerable<TEnum>(this IDataSet dataSet, IDataRow dataRow, string tableName,
        string dataNode, TEnum parserValue)
        where TEnum : struct, IConvertible
    {
        Type type = typeof(TEnum);
        string typeName = type.ToString().Substring(type.ToString().IndexOf("+") + 1);
        if (dataSet.Tables[tableName].Columns.Any(x => x.ColumnName == typeName))
        {
            string rawTableInput = dataRow.Field<string>(typeName);
            if (Enum.TryParse<TEnum>(rawTableInput, out var rti)) return rti;
            else
                return string.IsNullOrEmpty(rawTableInput)
                    ? default
                    : typeName switch
                    {
                        nameof(CashFlowPeriodicity) => (TEnum) ApplicationMessage.Log(Error.MissingInterpolationMethod,
                            dataNode),
                        nameof(InterpolationMethod) => (TEnum) ApplicationMessage.Log(Error.InvalidInterpolationMethod,
                            dataNode),
                        _ => (TEnum) ApplicationMessage.Log(Error.Generic, dataNode)
                    };
        }
        else return parserValue;
    }
}

public class ParsingStorage
{
    private readonly IDataSource dataSource;
    private readonly IWorkspace workspace;
    private readonly ImportArgs args;
    
    //Hierarchy Cache
    public Systemorph.Vertex.Hierarchies.IHierarchicalDimensionCache HierarchyCache;
    
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

public static class ImportTasks
{
    private static IDataSource DataSource { get; set; }

    public static void WithDataSource(IDataSource dataSource)
    {
        DataSource = dataSource;
    }

    public static async Task CleanAsync<T>(this IDataSource dataSource, Guid partitionId = default,
        Expression<Func<T, bool>> filter = null) where T : class, IPartitioned
    {
        var loadData = partitionId != (Guid) default
            ? await dataSource.Query<T>().Where(x => x.Partition == partitionId)
                .Where(filter ?? (Expression<Func<T, bool>>) (x => true)).ToListAsync()
            : await dataSource.Query<T>().Where(filter ?? (Expression<Func<T, bool>>) (x => true)).ToListAsync();
        await dataSource.DeleteAsync(loadData);
    }


    public static async Task CommitToAsync<TData, TPartition>(this IDataSource source, IDataSource target,
        Guid partitionId = default, bool snapshot = true, Expression<Func<TData, bool>> filter = null)
        where TData : class, IPartitioned
        where TPartition : IfrsPartition
    {
        if (partitionId != (Guid) default)
        {
            await target.Partition.SetAsync<TPartition>(partitionId);
            await source.Partition.SetAsync<TPartition>(partitionId);
        }

        if (snapshot) await CleanAsync<TData>(target, partitionId, filter);
        await target.UpdateAsync<TData>(await source.Query<TData>().ToArrayAsync());
        await target.CommitAsync();
    }


    public static ImportArgs GetArgsFromMain(IDataSet dataSet)
    {
        var mainTab = dataSet.Tables[Consts.Main];
        if (mainTab == null) ApplicationMessage.Log(Error.NoMainTab);
        if (!mainTab.Rows.Any()) ApplicationMessage.Log(Error.IncompleteMainTab);
        if (ApplicationMessage.HasErrors()) return null;

        var main = mainTab.Rows.First();
        var reportingNode =
            mainTab.Columns.Any(x => x.ColumnName == nameof(Args.ReportingNode)) &&
            main[nameof(Args.ReportingNode)] != null
                ? (string) main[nameof(ReportingNode)]
                : default(string);
        var scenario =
            mainTab.Columns.Any(x => x.ColumnName == nameof(Args.Scenario)) && main[nameof(Args.Scenario)] != null
                ? (string) main[nameof(Scenario)]
                : default(string);
        var year = mainTab.Columns.Any(x => x.ColumnName == nameof(Args.Year)) && main[nameof(Args.Year)] != null
            ? (int) Convert.ChangeType(main[nameof(Args.Year)], typeof(int))
            : default(int);
        var month = mainTab.Columns.Any(x => x.ColumnName == nameof(Args.Month)) && main[nameof(Args.Month)] != null
            ? (int) Convert.ChangeType(main[nameof(Args.Month)], typeof(int))
            : default(int);

        return new ImportArgs(reportingNode, year, month, default(Periodicity), scenario, default(string));
    }


    public async static void ValidateArgsForPeriodAsync(this ImportArgs args, IDataSource targetDataSource)
    {
        if (args.Year == default(int)) ApplicationMessage.Log(Error.YearInMainNotFound);
        if (args.Month == default(int)) ApplicationMessage.Log(Error.MonthInMainNotFound);
        var availableScenarios = await targetDataSource.Query<Scenario>().Select(x => x.SystemName).ToArrayAsync();
        if (!(args.Scenario == default(string) || availableScenarios.Contains(args.Scenario)))
            ApplicationMessage.Log(Error.DimensionNotFound, "Scenario", args.Scenario);
    }


    public static async Task CommitPartitionAsync<IPartition>(ImportArgs args, params IDataSource[] dataSources)
    {
        foreach (var dataSource in dataSources)
        {
            switch (typeof(IPartition).Name)
            {
                case nameof(PartitionByReportingNode):
                {
                    await dataSource.UpdateAsync<PartitionByReportingNode>(new[]
                    {
                        new PartitionByReportingNode
                        {
                            Id = (Guid) (await DataSource.Partition.GetKeyForInstanceAsync<PartitionByReportingNode>(
                                args)),
                            ReportingNode = args.ReportingNode
                        }
                    });
                    break;
                }
                case nameof(PartitionByReportingNodeAndPeriod):
                {
                    args.ValidateArgsForPeriodAsync(dataSource);
                    if (ApplicationMessage.HasErrors()) return;

                    await dataSource.UpdateAsync<PartitionByReportingNodeAndPeriod>(new[]
                    {
                        new PartitionByReportingNodeAndPeriod
                        {
                            Id = (Guid) (await DataSource.Partition
                                .GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(args)),
                            Year = args.Year,
                            Month = args.Month,
                            ReportingNode = args.ReportingNode,
                            Scenario = args.Scenario
                        }
                    });
                    break;
                }
                default:
                {
                    ApplicationMessage.Log(Error.PartitionTypeNotFound, typeof(IPartition).Name);
                    return;
                }
            }

            await dataSource.CommitAsync();
        }
    }


    public static async Task<ImportArgs> GetArgsAndCommitPartitionAsync<IPartition>(IDataSet dataSet,
        IDataSource targetDataSource)
    {
        var args = GetArgsFromMain(dataSet);
        if (ApplicationMessage.HasErrors()) return null;
        if (args.ReportingNode == default(string))
        {
            ApplicationMessage.Log(Error.ReportingNodeInMainNotFound);
            return null;
        }

        await CommitPartitionAsync<IPartition>(args, targetDataSource);
        return args;
    }


    public static async Task DataNodeFactoryAsync(IDataSet dataSet, string tableName, ImportArgs args,
        IDataSource targetDataSource)
    {
        var partition =
            (await DataSource.Query<PartitionByReportingNode>().Where(p => p.ReportingNode == args.ReportingNode)
                .ToArrayAsync()).FirstOrDefault();
        if (partition == null)
        {
            ApplicationMessage.Log(Error.ParsedPartitionNotFound, args.ReportingNode);
            return;
        }

        var table = dataSet.Tables[tableName];

        var dataNodesImported = table.Rows.Select(x => x.Field<string>(nameof(RawVariable.DataNode))).ToHashSet();
        var dataNodesDefined = await targetDataSource.Query<GroupOfContract>()
            .Where(x => dataNodesImported.Contains(x.SystemName)).ToArrayAsync();
        var dataNodeStatesDefined =
            await targetDataSource.Query<DataNodeState>().Select(x => x.DataNode).ToArrayAsync();
        var dataNodeParametersDefined =
            await targetDataSource.Query<SingleDataNodeParameter>().Select(x => x.DataNode).ToArrayAsync();
        var dataNodeStatesUndefined =
            dataNodesImported.Where(x => x != null && !dataNodeStatesDefined.Contains(x)).ToHashSet();
        var dataNodeSingleParametersUndefined = dataNodesImported.Where(x => x != null &&
                                                                             !dataNodeParametersDefined.Contains(x) &&
                                                                             dataNodesDefined.SingleOrDefault(y =>
                                                                                     y.SystemName == x) is
                                                                                 GroupOfInsuranceContract).ToHashSet();
        if ((dataNodeStatesUndefined?.Any() ?? false))
            await targetDataSource.UpdateAsync(dataNodeStatesUndefined.Select(x =>
                    new DataNodeState
                    {
                        DataNode = x,
                        Year = args.Year,
                        Month = Consts.DefaultDataNodeActivationMonth,
                        State = State.Active,
                        Partition = partition.Id
                    })
                .ToArray());
        if ((dataNodeSingleParametersUndefined?.Any() ?? false))
            await targetDataSource.UpdateAsync(dataNodeSingleParametersUndefined.Select(x =>
                    new SingleDataNodeParameter
                    {
                        DataNode = x,
                        Year = args.Year,
                        Month = Consts.DefaultDataNodeActivationMonth,
                        PremiumAllocation = Consts.DefaultPremiumExperienceAdjustmentFactor,
                        EconomicBasisDriver = ImportCalculationExtensions.GetDefaultEconomicBasisDriver(
                            dataNodesDefined.SingleOrDefault(y => y.SystemName == x)?.ValuationApproach,
                            dataNodesDefined.SingleOrDefault(y => y.SystemName == x)?.LiabilityType),
                        Partition = partition.Id
                    })
                .ToArray());
        await targetDataSource.CommitAsync();
    }


    public static async Task<ImportArgs[]> GetAllArgsAsync(ImportArgs args, IDataSource dataSource, string format)
    {
        ImportArgs[] allArgs;
        switch (format)
        {
            case ImportFormats.YieldCurve:
            {
                if (args.Scenario == null)
                {
                    var scenarios = await dataSource.Query<YieldCurve>()
                        .Where(x => x.Year == args.Year && x.Month == args.Month && x.Scenario != null)
                        .Select(x => x.Scenario).Distinct().ToArrayAsync();
                    var targetPartitions = await dataSource.Query<PartitionByReportingNodeAndPeriod>()
                        .Where(x => x.Year == args.Year && x.Month == args.Month && !scenarios.Contains(x.Scenario))
                        .OrderBy(x => x.Scenario).ToArrayAsync();
                    var targetScenarios = targetPartitions.Where(x => x.Scenario != null).Select(x => x.Scenario);
                    if (targetScenarios.Any())
                        ApplicationMessage.Log(Warning.ScenarioReCalculations, String.Join(", ", targetScenarios));
                    allArgs = targetPartitions.Select(x => new ImportArgs(x.ReportingNode, x.Year, x.Month,
                        default(Periodicity), x.Scenario, ImportFormats.Cashflow)).ToArray();
                }
                else
                {
                    allArgs = (await dataSource.Query<PartitionByReportingNodeAndPeriod>()
                            .Where(x => x.Year == args.Year && x.Month == args.Month && x.Scenario == null)
                            .ToArrayAsync())
                        .Select(x => new ImportArgs(x.ReportingNode, x.Year, x.Month, default(Periodicity),
                            args.Scenario, ImportFormats.Cashflow)).ToArray();
                }

                break;
            }
            case ImportFormats.DataNodeParameter:
            {
                if (args.Scenario != null)
                    return (args with {ImportFormat = ImportFormats.Cashflow}).RepeatOnce().ToArray();
                else
                {
                    var partitionByReportingNode = (await dataSource.Query<PartitionByReportingNode>()
                        .Where(x => x.ReportingNode == args.ReportingNode).ToArrayAsync()).Single().Id;
                    var scenarios = await dataSource.Query<DataNodeParameter>()
                        .Where(x => x.Partition == partitionByReportingNode && x.Year == args.Year &&
                                    x.Month == args.Month && x.Scenario != null).Select(x => x.Scenario).Distinct()
                        .ToArrayAsync();
                    var targetPartitions = await dataSource.Query<PartitionByReportingNodeAndPeriod>()
                        .Where(x => x.ReportingNode == args.ReportingNode && x.Year == args.Year &&
                                    x.Month == args.Month && !scenarios.Contains(x.Scenario)).OrderBy(x => x.Scenario)
                        .ToArrayAsync();
                    var targetScenarios = targetPartitions.Where(x => x.Scenario != null).Select(x => x.Scenario);
                    if (targetScenarios.Any())
                        ApplicationMessage.Log(Warning.ScenarioReCalculations, String.Join(", ", targetScenarios));
                    allArgs = targetPartitions.Select(x => new ImportArgs(x.ReportingNode, x.Year, x.Month,
                        default(Periodicity), x.Scenario, ImportFormats.Cashflow)).ToArray();
                }

                break;
            }
            default:
            {
                if (args.Scenario != null) return args.RepeatOnce().ToArray();
                var secondaryArgs = await dataSource.Query<PartitionByReportingNodeAndPeriod>()
                    .Where(x => x.ReportingNode == args.ReportingNode && x.Year == args.Year && x.Month == args.Month &&
                                x.Scenario != null)
                    .Select(x =>
                        new ImportArgs(x.ReportingNode, x.Year, x.Month, default(Periodicity), x.Scenario, format))
                    .ToArrayAsync();

                if (secondaryArgs.Any())
                    ApplicationMessage.Log(Warning.ScenarioReCalculations,
                        String.Join(", ", secondaryArgs.Select(x => x.Scenario)));
                allArgs = args.RepeatOnce().Concat(secondaryArgs).ToArray();
                break;
            }
        }

        return allArgs.Where(x => (!Scenarios.EnableScenario && x.Scenario == null) || Scenarios.EnableScenario)
            .ToArray();
    }


    public static async Task<ActivityLog> ComputeAsync(ImportArgs args, IWorkspace workspace,
        IWorkspace workspaceToCompute, bool saveRawVariables, IActivityVariable activity, IScopeFactory scopes)
    {
        activity.Start();
        var storage = new ImportStorage(args, workspaceToCompute, workspace);
        await storage.InitializeAsync();
        if (activity.HasErrors()) return activity.Finish();

        var universe = scopes.ForStorage(storage).ToScope<IModel>();
        var ivs = universe.GetScopes<ComputeAllScopes>(storage.DataNodesByImportScope[ImportScope.Primary])
            .SelectMany(x => x.CalculatedIfrsVariables);
        if (activity.HasErrors()) return activity.Finish();

        if (storage.DefaultPartition != storage.TargetPartition)
        {
            var bestEstimateIvs =
                await workspaceToCompute.LoadPartitionedDataAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(
                    storage.DefaultPartition);
            ivs = ivs.Where(iv => iv.Values.Any(y => Math.Abs(y) >= Consts.Precision)).ToArray()
                .Except(bestEstimateIvs, IfrsVariableComparer.Instance(ignoreValues: false))
                .Concat(bestEstimateIvs
                    .Intersect(ivs.Where(iv => iv.Values.All(y => Math.Abs(y) < Consts.Precision)).ToArray(),
                        IfrsVariableComparer.Instance(ignoreValues: true))
                    .Select(x => x with
                    {
                        Values = Enumerable.Repeat(0d, x.Values.Length).ToArray(),
                        Partition = storage.TargetPartition
                    }).ToArray());
        }

        workspace.Reset(x => x.ResetType<IfrsVariable>());
        await workspace.UpdateAsync<IfrsVariable>(ivs.Where(x =>
            storage.DefaultPartition != storage.TargetPartition || x.Values.Any(v => Math.Abs(v) >= Consts.Precision)));
        await workspace.CommitToAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(workspaceToCompute,
            storage.TargetPartition, snapshot: true,
            filter: x => storage.EstimateTypesByImportFormat[args.ImportFormat].Contains(x.EstimateType)
                         && storage.DataNodesByImportScope[ImportScope.Primary].Contains(x.DataNode));
        if (saveRawVariables)
        {
            if (args.Scenario == null)
                await workspace.DeleteAsync(await workspace.Query<RawVariable>()
                    .Where(rv => rv.Values.All(x => Math.Abs(x) < Consts.Precision)).ToArrayAsync());
            await workspace.CommitToAsync<RawVariable, PartitionByReportingNodeAndPeriod>(workspaceToCompute,
                storage.TargetPartition, snapshot: true,
                filter: x =>
                    storage.DataNodesByImportScope[ImportScope.Primary]
                        .Except(storage.DataNodesByImportScope[ImportScope.AddedToPrimary]).Contains(x.DataNode));
        }

        return activity.Finish();
    }


    public async static Task ValidateForDataNodeStateActiveAsync<T>(this IWorkspace workspace,
        Dictionary<string, DataNodeData> dataNodes) where T : BaseDataRecord
    {
        foreach (var item in (await workspace.Query<T>().ToArrayAsync()).GroupBy(x => x.DataNode))
            if (!dataNodes.ContainsKey(item.First().DataNode))
                ApplicationMessage.Log(Error.InactiveDataNodeState, item.First().DataNode);
    }



    public async static Task ValidateDataNodeStatesAsync(this IWorkspace workspace,
        Dictionary<string, DataNodeData> persistentDataNodeByDataNode)
    {
        foreach (var importedDataNodeState in await workspace.Query<DataNodeState>().ToArrayAsync())
        {
            if (persistentDataNodeByDataNode.TryGetValue(importedDataNodeState.DataNode,
                    out var currentPersistentDataNode))
            {
                if (importedDataNodeState.State < currentPersistentDataNode.State)
                    ApplicationMessage.Log(Error.ChangeDataNodeState, importedDataNodeState.DataNode,
                        currentPersistentDataNode.State.ToString(),
                        importedDataNodeState.State.ToString());

                if (importedDataNodeState.State == currentPersistentDataNode.State)
                    await workspace.DeleteAsync<DataNodeState>(importedDataNodeState);
            }
        }
    }


    public async static Task ValidateForMandatoryAocSteps(this IWorkspace workspace, IDataSet dataSet,
        HashSet<AocStep> mandatoryAocSteps)
    {
        var ignoreProperties = new[] {nameof(AocType), nameof(Novelty)};
        var missingAocStepsByIdentityProperties = (await workspace.Query<RawVariable>().ToListAsync())
            .GroupBy(x => x.ToIdentityString(ignoreProperties),
                x => new AocStep(x.AocType, x.Novelty),
                (properties, parsedAocSteps) => (properties, mandatoryAocSteps.Except(parsedAocSteps))
            );
        foreach ((var properties, var missingSteps) in missingAocStepsByIdentityProperties)
        foreach (var missingStep in missingSteps)
            ApplicationMessage.Log(Warning.MandatoryAocStepMissing, missingStep.AocType, missingStep.Novelty,
                properties);
    }



    public static async Task DefineAocFormats(IImportVariable import, IActivityVariable activity, IWorkspaceVariable work)
    {
        import.DefineFormat(ImportFormats.AocConfiguration, async (options, dataSet) =>
        {
            activity.Start();
            var workspace = work.CreateNew();
            workspace.InitializeFrom(options.TargetDataSource);

            var aocTypes = await options.TargetDataSource.Query<AocType>().OrderBy(x => x.Order).ToArrayAsync();
            var aocTypesCompulsory = typeof(AocTypes).GetFields().Select(x => (string) x.Name);
            // if(aocTypesCompulsory.Where(x => !aocTypes.Select(x => x.SystemName).Contains(x)).Any()) { //aocTypes const are not to be considered compulsory steps. 
            //     ApplicationMessage.Log(Error.AocTypeCompulsoryNotFound);
            //     return Activity.Finish();
            // }

            var logConfig = await import.FromDataSet(dataSet).WithType<AocConfiguration>().WithTarget(workspace)
                .ExecuteAsync();
            if (logConfig.Errors.Any()) return activity.Finish().Merge(logConfig);

            var orderByName = aocTypes.ToDictionary(x => x.SystemName, x => x.Order);

            var aocConfigs = (await workspace.Query<AocConfiguration>().ToArrayAsync())
                .GroupBy(x => (x.AocType, x.Novelty))
                .Select(y => y.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).FirstOrDefault())
                .ToDictionary(x => (x.AocType, x.Novelty));

            var aocOrder = aocConfigs.ToDictionary(x => x.Key, x => x.Value.Order);

            var newAocTypes = orderByName.Keys.Where(x => !aocConfigs.Keys.Contains((x, Novelties.I)) &&
                                                          !aocConfigs.Keys.Contains((x, Novelties.N)) &&
                                                          !aocConfigs.Keys.Contains((x, Novelties.C)) &&
                                                          !aocTypes.Any(y => y.Parent == x) &&
                                                          !aocTypesCompulsory.Contains(x)).ToArray();

            foreach (var newAocType in newAocTypes)
            {
                if (orderByName[newAocType] < orderByName[AocTypes.RCU])
                {
                    var step = (AocTypes.MC, Novelties.I);
                    await workspace.UpdateAsync(aocConfigs[step] with
                    {
                        AocType = newAocType, DataType = DataType.Optional, Order = ++aocOrder[step]
                    });
                }
                else if (orderByName[newAocType] > orderByName[AocTypes.RCU] &&
                         orderByName[newAocType] < orderByName[AocTypes.CF])
                {
                    var step = (AocTypes.RCU, Novelties.I);
                    await workspace.UpdateAsync(aocConfigs[step] with
                    {
                        AocType = newAocType, DataType = DataType.Optional, Order = ++aocOrder[step]
                    });
                }
                else if (orderByName[newAocType] > orderByName[AocTypes.IA] &&
                         orderByName[newAocType] < orderByName[AocTypes.YCU])
                {
                    foreach (var novelty in new[] {Novelties.I, Novelties.N})
                    {
                        var step = (AocTypes.AU, novelty);
                        var order = orderByName[newAocType] < orderByName[AocTypes.AU]
                            ? ++aocOrder[(AocTypes.IA, novelty)]
                            : ++aocOrder[(AocTypes.AU, novelty)];
                        await workspace.UpdateAsync(aocConfigs[step] with
                        {
                            AocType = newAocType, DataType = DataType.Optional, Order = order
                        });
                    }
                }
                else if (orderByName[newAocType] > orderByName[AocTypes.CRU] &&
                         orderByName[newAocType] < orderByName[AocTypes.CL])
                {
                    var stepI = (AocTypes.EV, Novelties.I);
                    var orderI = orderByName[newAocType] < orderByName[AocTypes.EV]
                        ? ++aocOrder[(AocTypes.CRU, Novelties.I)]
                        : ++aocOrder[(AocTypes.EV, Novelties.I)];
                    await workspace.UpdateAsync(aocConfigs[stepI] with
                    {
                        AocType = newAocType, DataType = DataType.Optional, Order = orderI
                    });

                    var stepN = (AocTypes.EV, Novelties.N);
                    var orderN = orderByName[newAocType] < orderByName[AocTypes.EV]
                        ? ++aocOrder[(AocTypes.AU, Novelties.N)]
                        : ++aocOrder[(AocTypes.EV, Novelties.N)];
                    await workspace.UpdateAsync(aocConfigs[stepN] with
                    {
                        AocType = newAocType, DataType = DataType.Optional, Order = orderN
                    });
                }
                else if (orderByName[newAocType] > orderByName[AocTypes.AM] &&
                         orderByName[newAocType] < orderByName[AocTypes.WO])
                {
                    var step = (AocTypes.WO, Novelties.C);
                    await workspace.UpdateAsync(aocConfigs[step] with
                    {
                        AocType = newAocType, DataType = DataType.Optional | DataType.CalculatedProjection,
                        Order = ++aocOrder[step]
                    });
                }
                else
                    ApplicationMessage.Log(Error.AocTypePositionNotSupported);
            }

            ;

            var aocConfigsFinal = await workspace.Query<AocConfiguration>().ToArrayAsync();
            if (aocConfigsFinal.GroupBy(x => x.Order).Any(x => x.Count() > 1))
                ApplicationMessage.Log(Error.AocConfigurationOrderNotUnique);

            await workspace.CommitToTargetAsync(options.TargetDataSource);
            workspace.Dispose();
            return activity.Finish().Merge(logConfig);
        });
    }

    public static async Task DefineYycFormat(IImportVariable import, IActivityVariable activity, IWorkspaceVariable work, IScopeFactory scopes)
    {
        import.DefineFormat(ImportFormats.YieldCurve, async (options, dataSet) =>
        {
            activity.Start();
            var primaryArgs = GetArgsFromMain(dataSet) with {ImportFormat = ImportFormats.YieldCurve};
            primaryArgs.ValidateArgsForPeriodAsync(options.TargetDataSource);
            if (!dataSet.Tables.Contains(primaryArgs.ImportFormat))
                ApplicationMessage.Log(Error.TableNotFound, primaryArgs.ImportFormat);
            if (ApplicationMessage.HasErrors()) return activity.Finish();
            var workspace = work.CreateNew();
            workspace.Initialize(x => x.FromSource(options.TargetDataSource)
                .DisableInitialization<RawVariable>()
                .DisableInitialization<IfrsVariable>());

            var committedYieldCurves = await options.TargetDataSource.Query<YieldCurve>().ToArrayAsync();
            var hasNameColumn = dataSet.Tables[ImportFormats.YieldCurve].Columns
                .Any(x => x.ColumnName == nameof(YieldCurve.Name));
            var log = await import.FromDataSet(dataSet).WithType<YieldCurve>(
                (dataset, datarow) =>
                {
                    var values = datarow.Table.Columns.Where(c => c.ColumnName.StartsWith(nameof(YieldCurve.Values)))
                        .OrderBy(c => c.ColumnName.Length).ThenBy(c => c.ColumnName)
                        .Select(x => datarow.Field<string>(x.ColumnName).CheckStringForExponentialAndConvertToDouble())
                        .ToArray().PruneButFirst();
                    return new YieldCurve
                    {
                        Currency = datarow.Field<string>(nameof(YieldCurve.Currency)),
                        Year = primaryArgs.Year,
                        Month = primaryArgs.Month,
                        Scenario = primaryArgs.Scenario,
                        Values = !values.Any() ? new[] {0d} : values,
                        Name = hasNameColumn ? datarow.Field<string>(nameof(YieldCurve.Name)) : default(string)
                    };
                }
            ).WithTarget(workspace).ExecuteAsync();

            if (log.Errors.Any()) return activity.Finish().Merge(log);
            var toCommitYieldCurves =
                (await workspace.Query<YieldCurve>().ToArrayAsync()).Except(committedYieldCurves,
                    YieldCurveComparer.Instance());
            if (!toCommitYieldCurves.Any())
            {
                ApplicationMessage.Log(Warning.VariablesAlreadyImported);
                return activity.Finish().Merge(log);
            }

            var allArgs = await GetAllArgsAsync(primaryArgs, options.TargetDataSource, ImportFormats.YieldCurve);
            var updatedCurrencies = toCommitYieldCurves.Select(x => x.Currency).Distinct();
            var dataNodesToUpdate = await workspace.Query<GroupOfContract>()
                .Where(x => updatedCurrencies.Contains(x.ContractualCurrency)).Select(x => x.SystemName).ToArrayAsync();
            var workspaceToCompute = work.CreateNew();
            workspaceToCompute.Initialize(x => x.FromSource(options.TargetDataSource));
            foreach (var args in allArgs)
            {
                await CommitPartitionAsync<PartitionByReportingNodeAndPeriod>(args, workspace, workspaceToCompute);
                var targetPartition = (Guid) (await options.TargetDataSource.Partition
                    .GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(args));
                var defaultPartition =
                    (Guid) (await options.TargetDataSource.Partition
                        .GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(args with {Scenario = null}));
                if (ApplicationMessage.HasErrors()) return activity.Finish().Merge(log);

                // Avoid starting the computation if no best estimate cash flow has ever been imported 
                if (args.Scenario == null)
                {
                    await options.TargetDataSource.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(null);
                    if (!(await options.TargetDataSource.Query<RawVariable>().Where(x => x.Partition == targetPartition)
                            .Take(1).ToArrayAsync()).Any()) continue;
                }
                // Remove data nodes which are unaffected by the updated yield curves
                // TODO : Reintroduce this functionality. Note all UpdateAsync/DeleteAsync performed to the workspaceToCompute are then trasferred to the DataSource.
                //        This is way this functionality should be written in a different way. 
                // await workspaceToCompute.DeleteAsync( await workspaceToCompute.Query<RawVariable>()
                //     .Where(x => !(dataNodesToUpdate.Contains(x.DataNode) && (x.Partition == targetPartition || x.Partition == defaultPartition))).ToArrayAsync() );

                log = log.Merge(await ComputeAsync(args, work, workspaceToCompute, false, activity, scopes));
                if (log.Errors.Any()) return activity.Finish().Merge(log);
            }

            await workspaceToCompute.UpdateAsync<YieldCurve>(toCommitYieldCurves);
            await workspaceToCompute.CommitToTargetAsync(options.TargetDataSource);
            return activity.Finish().Merge(log);
        });
    }


    public static async Task<ActivityLog> UploadDataNodesToWorkspaceAsync(IDataSet dataSet, IWorkspace workspace, IDataSource targetDataSource, IActivityVariable activity, IImportVariable import)
    {
        workspace.Reset(x => x.ResetInitializationRules().ResetCurrentPartitions());
        workspace.Initialize(x => x.FromSource(targetDataSource)
                               .DisableInitialization<RawVariable>()
                               .DisableInitialization<IfrsVariable>()
                               .DisableInitialization<DataNodeState>()
                               .DisableInitialization<DataNodeParameter>());
    
        activity.Start();
        var args = await GetArgsAndCommitPartitionAsync<PartitionByReportingNode>(dataSet, targetDataSource);

        if(activity.HasErrors()) return activity.Finish();
    
        var storage = new ParsingStorage(args, targetDataSource, workspace);
        await storage.InitializeAsync();
        if(activity.HasErrors()) return activity.Finish();
       
        var importLogPortfolios = await import.FromDataSet(dataSet)
            .WithType<InsurancePortfolio>((dataset, datarow) => {
                var pf = new InsurancePortfolio { SystemName = datarow.Field<string>(nameof(DataNode.SystemName)),
                                     DisplayName = datarow.Field<string>(nameof(DataNode.DisplayName)),
                                     Partition = storage.TargetPartitionByReportingNode.Id,
                                     ContractualCurrency = datarow.Field<string>(nameof(DataNode.ContractualCurrency)),
                                     FunctionalCurrency = storage.ReportingNode.Currency,
                                     LineOfBusiness = datarow.Field<string>(nameof(DataNode.LineOfBusiness)),
                                     ValuationApproach = datarow.Field<string>(nameof(DataNode.ValuationApproach)),
                                     OciType = datarow.Field<string>(nameof(DataNode.OciType)) };
                return ImportCalculationExtensions.ExtendPortfolio(pf, datarow);
            })
            .WithType<ReinsurancePortfolio>((dataset, datarow) => {
                var pf = new ReinsurancePortfolio {  SystemName = datarow.Field<string>(nameof(DataNode.SystemName)),
                                        DisplayName = datarow.Field<string>(nameof(DataNode.DisplayName)),
                                        Partition = storage.TargetPartitionByReportingNode.Id,
                                        ContractualCurrency = datarow.Field<string>(nameof(DataNode.ContractualCurrency)),
                                        FunctionalCurrency = storage.ReportingNode.Currency,
                                        LineOfBusiness = datarow.Field<string>(nameof(DataNode.LineOfBusiness)),
                                        ValuationApproach = datarow.Field<string>(nameof(DataNode.ValuationApproach)),
                                        OciType = datarow.Field<string>(nameof(DataNode.OciType)) };
                return ImportCalculationExtensions.ExtendPortfolio(pf, datarow);
            })
            .WithTarget(workspace)
            .ExecuteAsync();
    
        var portfolios = await workspace.Query<Portfolio>().ToDictionaryAsync(x => x.SystemName);
        var yieldCurveColumnGroupOfInsuranceContract = dataSet.Tables.Contains(nameof(GroupOfInsuranceContract)) && dataSet.Tables[nameof(GroupOfInsuranceContract)].Columns.Any(x => x.ColumnName == nameof(GroupOfInsuranceContract.YieldCurveName));
        var yieldCurveColumnGroupOfReinsuranceContract = dataSet.Tables.Contains(nameof(GroupOfReinsuranceContract)) && dataSet.Tables[nameof(GroupOfReinsuranceContract)].Columns.Any(x => x.ColumnName == nameof(GroupOfReinsuranceContract.YieldCurveName));

        var importLogGroupOfContracts = await import.FromDataSet(dataSet)
            .WithType<GroupOfInsuranceContract>((dataset, datarow) => {
                var gicSystemName = datarow.Field<string>(nameof(DataNode.SystemName));
                var pf = datarow.Field<string>(nameof(InsurancePortfolio));
                if(!portfolios.TryGetValue(pf, out var portfolioData)) {
                    ApplicationMessage.Log(Error.PortfolioGicNotFound, pf, gicSystemName);
                    return null;
            }
            var gic = new GroupOfInsuranceContract { SystemName = gicSystemName,
                                                     DisplayName = datarow.Field<string>(nameof(DataNode.DisplayName)),
                                                     Partition = storage.TargetPartitionByReportingNode.Id,
                                                     ContractualCurrency = portfolioData.ContractualCurrency,
                                                     FunctionalCurrency = portfolioData.FunctionalCurrency,
                                                     LineOfBusiness = portfolioData.LineOfBusiness,
                                                     ValuationApproach = portfolioData.ValuationApproach,
                                                     OciType = portfolioData.OciType,
                                                     AnnualCohort =  Convert.ToInt32(datarow.Field<object>(nameof(GroupOfContract.AnnualCohort))),
                                                     LiabilityType = datarow.Field<string>(nameof(GroupOfContract.LiabilityType)),
                                                     Profitability = datarow.Field<string>(nameof(GroupOfContract.Profitability)),
                                                     Portfolio = pf,
                                                     YieldCurveName = yieldCurveColumnGroupOfInsuranceContract
                                                                    ? datarow.Field<string>(nameof(GroupOfContract.YieldCurveName)) 
                                                                    : (string)null };
            return ImportCalculationExtensions.ExtendGroupOfContract(gic, datarow);
        })
        .WithType<GroupOfReinsuranceContract>((dataset, datarow) => {
            var gricSystemName = datarow.Field<string>(nameof(DataNode.SystemName));
            var pf = datarow.Field<string>(nameof(ReinsurancePortfolio));
            if(!portfolios.TryGetValue(pf, out var portfolioData)) {
                ApplicationMessage.Log(Error.PortfolioGicNotFound, pf, gricSystemName);
                return null;
            }
            var gric = new GroupOfReinsuranceContract { SystemName = gricSystemName,
                                                        DisplayName = datarow.Field<string>(nameof(DataNode.DisplayName)),
                                                        Partition = storage.TargetPartitionByReportingNode.Id,
                                                        ContractualCurrency = portfolioData.ContractualCurrency,
                                                        FunctionalCurrency = portfolioData.FunctionalCurrency,
                                                        LineOfBusiness = portfolioData.LineOfBusiness,
                                                        ValuationApproach = portfolioData.ValuationApproach,
                                                        OciType = portfolioData.OciType,
                                                        AnnualCohort = Convert.ToInt32(datarow.Field<object>(nameof(GroupOfContract.AnnualCohort))),
                                                        LiabilityType = datarow.Field<string>(nameof(GroupOfContract.LiabilityType)),
                                                        Profitability = datarow.Field<string>(nameof(GroupOfContract.Profitability)),
                                                        Portfolio = pf,
                                                        Partner = datarow.Field<string>(nameof(GroupOfContract.Partner)),
                                                        YieldCurveName = yieldCurveColumnGroupOfReinsuranceContract
                                                                        ? datarow.Field<string>(nameof(GroupOfContract.YieldCurveName)) 
                                                                        : (string)null };
            return ImportCalculationExtensions.ExtendGroupOfContract(gric, datarow);
        })
        .WithTarget(workspace)
        .ExecuteAsync();
   
        return activity.Finish().Merge(importLogPortfolios).Merge(importLogGroupOfContracts);
    }


    public static async Task DefineDataNodeFormat(IImportVariable import, IActivityVariable activity, IWorkspaceVariable work)
    {
        import.DefineFormat(ImportFormats.DataNode, async (options, dataSet) =>
        {
            var workspace = work.CreateNew();
            var log = await UploadDataNodesToWorkspaceAsync(dataSet, workspace, options.TargetDataSource, activity, import);
            var partition = (Guid) workspace.Partition.GetCurrent(nameof(PartitionByReportingNode));
            await workspace.CommitToAsync<InsurancePortfolio, PartitionByReportingNode>(options.TargetDataSource,
                partition);
            await workspace.CommitToAsync<ReinsurancePortfolio, PartitionByReportingNode>(options.TargetDataSource,
                partition);
            await workspace.CommitToAsync<GroupOfInsuranceContract, PartitionByReportingNode>(options.TargetDataSource,
                partition);
            await workspace.CommitToAsync<GroupOfReinsuranceContract, PartitionByReportingNode>(
                options.TargetDataSource, partition);
            return log;
        });
    }


    public static async Task<ActivityLog> UploadDataNodeStateToWorkspaceAsync(IDataSet dataSet, IWorkspace workspace, IDataSource targetDataSource, IActivityVariable activity, IImportVariable import)
    {
        workspace.Reset(x => x.ResetInitializationRules().ResetCurrentPartitions());
        workspace.Initialize(x => x.FromSource(targetDataSource)
                               .DisableInitialization<RawVariable>()
                               .DisableInitialization<IfrsVariable>()
                               .DisableInitialization<DataNodeState>());
        activity.Start();
        var args = await GetArgsAndCommitPartitionAsync<PartitionByReportingNodeAndPeriod>(dataSet, targetDataSource) with {ImportFormat = ImportFormats.DataNodeState};
        if (!dataSet.Tables.Contains(args.ImportFormat)) ApplicationMessage.Log(Error.TableNotFound, args.ImportFormat);
        if(activity.HasErrors()) return activity.Finish();
    
        var storage = new ParsingStorage(args, targetDataSource, workspace);
        await storage.InitializeAsync();
        if(activity.HasErrors()) return activity.Finish();

        var importLog = await import.FromDataSet(dataSet).WithType<DataNodeState>(
            (dataset, datarow) => new DataNodeState {
                DataNode = datarow.Field<string>(nameof(DataNodeState.DataNode)),
                State = (State)Enum.Parse(typeof(State), datarow.Field<string>(nameof(DataNodeState.State))),
                Year = args.Year,
                Month = args.Month,
                Partition = storage.TargetPartitionByReportingNode.Id
            }
        ).WithTarget(workspace).ExecuteAsync();

        await workspace.ValidateDataNodeStatesAsync(storage.DataNodeDataBySystemName);
        return activity.Finish().Merge(importLog);
    }

    public static async Task DefineDataNodeStateFormat(IImportVariable import, IWorkspaceVariable work, IActivityVariable activity)
    {
        import.DefineFormat(ImportFormats.DataNodeState, async (options, dataSet) =>
        {
            var workspace = work.CreateNew();
            var log = await UploadDataNodeStateToWorkspaceAsync(dataSet, workspace, options.TargetDataSource, activity, import);
            await workspace.CommitToAsync<DataNodeState, PartitionByReportingNode>(options.TargetDataSource,
                (Guid) workspace.Partition.GetCurrent(nameof(PartitionByReportingNode)), snapshot: false);
            return log;
        });
    }


    public static async Task<ActivityLog> UploadDataNodeParameterToWorkspaceAsync(IDataSet dataSet, ImportArgs args, IWorkspace workspace, IDataSource targetDataSource, IActivityVariable activity, IImportVariable import)
    {
        activity.Start();
        var storage = new ParsingStorage(args, targetDataSource, workspace);
        await storage.InitializeAsync();
        if(activity.HasErrors()) return activity.Finish();
        var singleDataNode = new List<string>();
        var interDataNode = new List<(string,string)>();

        var hasCashFlowPeriodicityColumn = dataSet.Tables[nameof(SingleDataNodeParameter)].Columns.Any(x => x.ColumnName == nameof(SingleDataNodeParameter.CashFlowPeriodicity));
        var hasInterpolationMethodColumn = dataSet.Tables[nameof(SingleDataNodeParameter)].Columns.Any(x => x.ColumnName == nameof(SingleDataNodeParameter.InterpolationMethod));
        var hasEconomicBasisDriverColumn = dataSet.Tables[nameof(SingleDataNodeParameter)].Columns.Any(x => x.ColumnName == nameof(SingleDataNodeParameter.EconomicBasisDriver));
        var hasReleasePattern = dataSet.Tables[nameof(SingleDataNodeParameter)].Columns.Any(x => x.ColumnName.StartsWith(nameof(SingleDataNodeParameter.ReleasePattern)));
        var hasPremiumAllocation = dataSet.Tables[nameof(SingleDataNodeParameter)].Columns.Any(x => x.ColumnName == nameof(SingleDataNodeParameter.PremiumAllocation));

        var importLog = await import.FromDataSet(dataSet)
                                .WithType<SingleDataNodeParameter>( (dataset, datarow) => {

                                    //read and validate DataNodes
                                    var dataNode = datarow.Field<string>(nameof(DataNode));
                                    if(!storage.IsValidDataNode(dataNode)) { ApplicationMessage.Log(Error.InvalidDataNode, dataNode); return null; }

                                    //check for duplicates
                                    if(singleDataNode.Contains(dataNode)) { ApplicationMessage.Log(Error.DuplicateSingleDataNode, dataNode); return null; }
                                    singleDataNode.Add(dataNode);
                                   
                                    CashFlowPeriodicity cashFlowPeriodicity = default;
                                    if (hasCashFlowPeriodicityColumn)
                                        if ( Enum.TryParse(datarow.Field<string>(nameof(SingleDataNodeParameter.CashFlowPeriodicity)), out CashFlowPeriodicity cfp))
                                            cashFlowPeriodicity = cfp;
                                        else { ApplicationMessage.Log(Error.InvalidCashFlowPeriodicity, dataNode); return null; }

                                    InterpolationMethod interpolationMethod = default;
                                    if(hasInterpolationMethodColumn)
                                        {
                                            var interpolationMethodInput = datarow.Field<string>(nameof(SingleDataNodeParameter.InterpolationMethod));
                                            if ( Enum.TryParse(interpolationMethodInput, out InterpolationMethod ipm)) 
                                                interpolationMethod = ipm;
                                            else if ( !(cashFlowPeriodicity == (CashFlowPeriodicity)default && string.IsNullOrEmpty(interpolationMethodInput)) ) { ApplicationMessage.Log(Error.InvalidInterpolationMethod, dataNode); return null; }
                                    }
                                    
                                    string economicBasisDriverInput = default;
                                    if(hasEconomicBasisDriverColumn)
                                        economicBasisDriverInput = datarow.Field<string>(nameof(SingleDataNodeParameter.EconomicBasisDriver));
                                    var economicBasisDriver = storage.ValidateEconomicBasisDriver(economicBasisDriverInput, dataNode);                                    

                                    double[] releasePattern = default;
                                    if(hasReleasePattern)
                                        releasePattern = datarow.Table.Columns
                                            .Where(c => c.ColumnName.StartsWith(nameof(SingleDataNodeParameter.ReleasePattern)))
                                            .OrderBy(c => c.ColumnName.Length).ThenBy(c => c.ColumnName)
                                            .Select(x => datarow.Field<string>(x.ColumnName).CheckStringForExponentialAndConvertToDouble())
                                            .ToArray().Prune();
            
                                    //Instantiate SingleDataNodeParameter
                                    var singleDataNodeParameter = new SingleDataNodeParameter {
                                        Year = args.Year,
                                        Month = args.Month,
                                        Scenario = args.Scenario,
                                        Partition = storage.TargetPartitionByReportingNode.Id,
                                        DataNode = dataNode,
                                        CashFlowPeriodicity = cashFlowPeriodicity,
                                        InterpolationMethod = interpolationMethod,
                                        EconomicBasisDriver = economicBasisDriver,
                                        ReleasePattern = releasePattern,
                                        // Modify here, it should be optional rather than mandatory - A.K. 
                                        PremiumAllocation = hasPremiumAllocation ? (datarow.Field<object>(nameof(SingleDataNodeParameter.PremiumAllocation)))
                                                                .ToString().CheckStringForExponentialAndConvertToDouble() : 0,
                                    };
                                    return ImportCalculationExtensions.ExtendSingleDataNodeParameter(singleDataNodeParameter, datarow);
                                })
                                .WithType<InterDataNodeParameter>( (dataset, datarow) => {

                                    //read and validate DataNodes
                                    var dataNode = datarow.Field<string>(nameof(InterDataNodeParameter.DataNode));
                                    if(!storage.IsValidDataNode(dataNode)) { ApplicationMessage.Log(Error.InvalidDataNode, dataNode); return null; }

                                    var linkedDataNode = datarow.Field<string>(nameof(InterDataNodeParameter.LinkedDataNode));
                                    if(!storage.IsValidDataNode(linkedDataNode)) { ApplicationMessage.Log(Error.InvalidDataNode, linkedDataNode); return null; }
                                    var dataNodes = new string[]{dataNode, linkedDataNode}.OrderBy(x => x).ToArray();

                                    //validate ReinsuranceGross Link
                                    var isDn1Reinsurance = storage.IsDataNodeReinsurance(dataNodes[0]);
                                    var isDn2Reinsurance = storage.IsDataNodeReinsurance(dataNodes[1]);
                                    var isGrossReinsuranceLink = (isDn1Reinsurance && !isDn2Reinsurance) != (!isDn1Reinsurance && isDn2Reinsurance);
                                    var reinsCov = (datarow.Field<object>(nameof(InterDataNodeParameter.ReinsuranceCoverage)))
                                                        .ToString().CheckStringForExponentialAndConvertToDouble();
                                    if(!isGrossReinsuranceLink && Math.Abs(reinsCov) > Consts.Precision )
                                        ApplicationMessage.Log(Error.ReinsuranceCoverageDataNode, dataNodes[0], dataNodes[1]);  // TODO: is this error or warning?

                                    //check for duplicates
                                    if(interDataNode.Contains((dataNodes[0], dataNodes[1])) || interDataNode.Contains((dataNodes[1], dataNodes[0])))
                                        ApplicationMessage.Log(Error.DuplicateInterDataNode, dataNodes[0], dataNodes[1]);  // TODO: is this error or warning?

                                    interDataNode.Add((dataNodes[0], dataNodes[1])); 
                                    //Instantiate InterDataNodeParameter
                                    return new InterDataNodeParameter {
                                        Year = args.Year,
                                        Month = args.Month,
                                        Scenario = args.Scenario,
                                        Partition = storage.TargetPartitionByReportingNode.Id,
                                        DataNode = dataNodes[0],
                                        LinkedDataNode = dataNodes[1],
                                        ReinsuranceCoverage = reinsCov,
                                    };
                                })
                                .WithTarget(workspace)
                                .ExecuteAsync();
    
        return activity.Finish().Merge(importLog);
    }

    public static async Task DefineDataNodeParameterFormat(IImportVariable import, IActivityVariable activity, IWorkspaceVariable work, IScopeFactory scopes)
    {
        import.DefineFormat(ImportFormats.DataNodeParameter, async (options, dataSet) =>
        {
            activity.Start();
            var primaryArgs = GetArgsFromMain(dataSet) with {ImportFormat = ImportFormats.DataNodeParameter};
            primaryArgs.ValidateArgsForPeriodAsync(options.TargetDataSource);
            if (!dataSet.Tables.Contains(nameof(SingleDataNodeParameter)) &&
                !dataSet.Tables.Contains(nameof(InterDataNodeParameter)))
                ApplicationMessage.Log(Error.TableNotFound, nameof(SingleDataNodeParameter),
                    nameof(InterDataNodeParameter));
            if (ApplicationMessage.HasErrors()) return activity.Finish();
            var workspace = work.CreateNew();
            workspace.Initialize(x =>
                x.FromSource(options.TargetDataSource).DisableInitialization<RawVariable>()
                    .DisableInitialization<IfrsVariable>());

            var committedParameters = await options.TargetDataSource.Query<DataNodeParameter>().ToArrayAsync();
            var log = await UploadDataNodeParameterToWorkspaceAsync(dataSet, primaryArgs, workspace, 
                                                                    options.TargetDataSource, activity, 
                                                                    import);

            if (log.Errors.Any()) return activity.Finish().Merge(log);
            var toCommitParameters =
                (await workspace.Query<DataNodeParameter>().ToArrayAsync()).Except(committedParameters,
                    ParametersComparer.Instance());
            if (!toCommitParameters.Any())
            {
                ApplicationMessage.Log(Warning.VariablesAlreadyImported);
                return activity.Finish().Merge(log);
            }

            var allArgs = await GetAllArgsAsync(primaryArgs, options.TargetDataSource, ImportFormats.DataNodeParameter);
            var targetDataNodes = toCommitParameters.Select(x => x.DataNode)
                .Concat(toCommitParameters.Where(x => x is InterDataNodeParameter)
                    .Select(x => ((InterDataNodeParameter) x).LinkedDataNode)).ToHashSet();
            var workspaceToCompute = work.CreateNew();
            workspaceToCompute.Initialize(x =>
                x.FromSource(options.TargetDataSource).DisableInitialization<RawVariable>());

            foreach (var args in allArgs)
            {
                await CommitPartitionAsync<PartitionByReportingNodeAndPeriod>(args, workspace, workspaceToCompute);
                var targetPartition = (Guid) (await options.TargetDataSource.Partition
                    .GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(args));
                var defaultPartition =
                    (Guid) (await options.TargetDataSource.Partition
                        .GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(args with {Scenario = null}));
                var previousPartition =
                    (Guid) (await options.TargetDataSource.Partition
                        .GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(args with
                        {
                            Scenario = null, Year = args.Year - 1, Month = 12
                        }));
                await options.TargetDataSource.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(null);
                if (ApplicationMessage.HasErrors()) return activity.Finish().Merge(log);

                // Avoid starting the computation if no best estimate cash flow or actuals has ever been imported 
                if (!(await options.TargetDataSource.Query<RawVariable>().Where(x => x.Partition == defaultPartition)
                        .Take(1).ToArrayAsync()).Any()) continue;

                // Only nominals corresponding to the target data nodes are added to the workspace
                var nominals = await options.TargetDataSource.Query<RawVariable>().Where(x =>
                    targetDataNodes.Contains(x.DataNode) &&
                    (x.Partition == targetPartition || x.Partition == defaultPartition ||
                     x.Partition == previousPartition)).ToArrayAsync();
                if (nominals.Any()) await workspaceToCompute.UpdateAsync(nominals);

                log = log.Merge(await ComputeAsync(args, workspace, workspaceToCompute, false, activity, scopes));
                if (log.Errors.Any()) return activity.Finish().Merge(log);
            }

            await workspaceToCompute.UpdateAsync(toCommitParameters);
            await workspaceToCompute.CommitToTargetAsync(options.TargetDataSource);
            return activity.Finish().Merge(log);
        });
    }


    public static async Task<ActivityLog> ParseCashflowsToWorkspaceAsync(IDataSet dataSet, ImportArgs args, IWorkspace workspace, IDataSource targetDataSource, IActivityVariable activity, IImportVariable import)
    {
        workspace.Reset(x => x.ResetInitializationRules().ResetCurrentPartitions());
        workspace.Initialize(x => x.FromSource(targetDataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());
    
        activity.Start();
        var parsingStorage = new ParsingStorage(args, targetDataSource, workspace);
        await parsingStorage.InitializeAsync();
        if(activity.HasErrors()) return activity.Finish();
    
        //var hasAccidentYearColumn = dataSet.Tables[ImportFormats.Cashflow].Columns.Any(x => x.ColumnName == nameof(RawVariable.AccidentYear));
        //var hasCashFlowPeriodicityColumn = dataSet.Tables[ImportFormats.Cashflow].Columns.Any(x => x.ColumnName == nameof(CashFlowPeriodicity));
        //var hasInterpolationMethodColumn = dataSet.Tables[ImportFormats.Cashflow].Columns.Any(x => x.ColumnName == nameof(InterpolationMethod));
    
        var importLog = await import.FromDataSet(dataSet)
            .WithType<RawVariable> ( (dataset, datarow) => {
                var aocType = datarow.Field<string>(nameof(RawVariable.AocType));
                var novelty = datarow.Field<string>(nameof(RawVariable.Novelty));
                var dataNode = datarow.Field<string>(nameof(DataNode));

                //if(hasCashFlowPeriodicityColumn && !hasInterpolationMethodColumn) { ApplicationMessage.Log(Error.MissingInterpolationMethod, dataNode); return null; };
                if (dataSet.Tables[ImportFormats.Cashflow].Columns.Any(x => x.ColumnName == nameof(RawVariable.AccidentYear)) && 
                    dataSet.Tables[ImportFormats.Cashflow].Columns.Any(x => x.ColumnName == nameof(InterpolationMethod))){
                        ApplicationMessage.Log(Error.MissingInterpolationMethod, dataNode);
                        return null;
                    }

                // CashflowPeriodicity given by the Cashflows or else taken from the SingleDataNodeParameters
                CashFlowPeriodicity cashFlowPeriodicity = dataset.ParseEnumerable<CashFlowPeriodicity>(datarow, ImportFormats.Cashflow, 
                                                                                                dataNode, parsingStorage.GetCashFlowPeriodicity(dataNode));
            /*if (hasCashFlowPeriodicityColumn)
            {
                var cashFlowPeriodicityInput = datarow.Field<string>(nameof(SingleDataNodeParameter.CashFlowPeriodicity)); 
                if (Enum.TryParse(cashFlowPeriodicityInput, out CashFlowPeriodicity cfp))
                    cashFlowPeriodicity = cfp;
                else if (!string.IsNullOrEmpty(cashFlowPeriodicityInput)) {ApplicationMessage.Log(Error.InvalidCashFlowPeriodicity, dataNode); return null;}
            }
            else cashFlowPeriodicity = parsingStorage.GetCashFlowPeriodicity(dataNode); */

            // InterpolationMethod, if needed by CashflowPeriodicity, given by the Cashflows or else taken from the SingleDataNodeParameters
                InterpolationMethod interpolationMethod = dataset.ParseEnumerable<InterpolationMethod>(datarow, ImportFormats.Cashflow, 
                                                                                                dataNode, parsingStorage.GetInterpolationMethod(dataNode));
            /*if(cashFlowPeriodicity != new CashFlowPeriodicity()) 
            {
                if(hasInterpolationMethodColumn)
                {
                    var interpolationMethodInput = datarow.Field<string>(nameof(SingleDataNodeParameter.InterpolationMethod));
                    if ( Enum.TryParse(interpolationMethodInput, out InterpolationMethod ipm)) 
                        interpolationMethod = ipm;
                    else if(!string.IsNullOrEmpty(interpolationMethodInput))
                        {ApplicationMessage.Log(Error.InvalidInterpolationMethod, dataNode); return null;}
                    else 
                        interpolationMethod = parsingStorage.GetInterpolationMethod(dataNode);
                }
                else interpolationMethod = parsingStorage.GetInterpolationMethod(dataNode);
            }*/

            if(!parsingStorage.DataNodeDataBySystemName.TryGetValue(dataNode, out var dataNodeData)) {
                ApplicationMessage.Log(Error.InvalidDataNode, dataNode);
                return null;
            }

            // Error if AocType is not present in the mapping
            if(!parsingStorage.AocTypeMap.Contains(new AocStep(aocType, novelty))) {
                ApplicationMessage.Log(Error.AocTypeMapNotFound, aocType, novelty);
                return null;
            }
                        
            // Filter out cash flows for DataNode that were created in the past and are still active and come with AocType = BOPI
            if(dataNodeData.Year < args.Year && aocType == AocTypes.BOP && novelty == Novelties.I) {
                ApplicationMessage.Log(Warning.ActiveDataNodeWithCashflowBOPI, dataNode);
                return null;
            }

            int? accidentYear = dataset.ParseAccidentYear(datarow, ImportFormats.Cashflow);
            /*if(hasAccidentYearColumn && datarow.Field<string>(nameof(RawVariable.AccidentYear)) != null) {
                if(!Int32.TryParse(datarow.Field<string>(nameof(RawVariable.AccidentYear)), out var parsedAccidentYear)) {
                //ApplicationMessage.Log(Error.AccidentYearTypeNotValid, datarow.Field<string>(nameof(RawVariable.AccidentYear))); return null;",
                }
                else
                accidentYear = (int?)parsedAccidentYear;
            } */

            (string AmountType, string EstimateType) valueType = datarow.ParseAmountAndEstimateType(ImportFormats.Cashflow, parsingStorage.DimensionsWithExternalId, parsingStorage.EstimateType, parsingStorage.AmountType);
            var values = datarow.Table.Columns.Where(c => c.ColumnName.StartsWith(nameof(RawVariable.Values))).OrderBy(c => c.ColumnName.Length).ThenBy(c => c.ColumnName)
                                .Select(x => datarow.Field<string>(x.ColumnName).CheckStringForExponentialAndConvertToDouble()).ToArray();

            // Extra adjustment to values
            values = values.AdjustValues(args, dataNodeData, accidentYear);

            // Filter out empty raw variables for AocStep \not\in MandatoryAocSteps
            if(args.Scenario == null) {
                values = values.Prune();
                if(values.Length == 0 && !parsingStorage.MandatoryAocSteps.Contains(new AocStep(aocType, novelty))) return null;
            }
            
            var item = new RawVariable {
                DataNode = dataNode,
                AocType = aocType,
                Novelty = novelty,
                AmountType = valueType.AmountType,
                EstimateType = valueType.EstimateType,
                AccidentYear = accidentYear,
                Partition = parsingStorage.TargetPartitionByReportingNodeAndPeriod.Id,
                Values = ArithmeticOperations.Multiply(ImportCalculationExtensions.GetSign(ImportFormats.Cashflow, (aocType, valueType.AmountType, valueType.EstimateType, dataNodeData.IsReinsurance), parsingStorage.HierarchyCache), values)
                            .Interpolate(cashFlowPeriodicity, interpolationMethod)
            };
            return item;
        }, ImportFormats.Cashflow
        ).WithTarget(workspace).ExecuteAsync();
    
        await workspace.ExtendParsedVariables(parsingStorage.HierarchyCache);
        await workspace.ValidateForMandatoryAocSteps(dataSet, parsingStorage.MandatoryAocSteps);
        await workspace.ValidateForDataNodeStateActiveAsync<RawVariable>(parsingStorage.DataNodeDataBySystemName);
        return activity.Finish().Merge(importLog);
    }


    public static async Task DefineCashflowFormat(IImportVariable import, IActivityVariable activity, IWorkspaceVariable work, IScopeFactory scopes)
    {
        import.DefineFormat(ImportFormats.Cashflow, async (options, dataSet) =>
        {
            activity.Start();
            var primaryArgs =
                await GetArgsAndCommitPartitionAsync<PartitionByReportingNodeAndPeriod>(dataSet,
                        options.TargetDataSource) with
                    {
                        ImportFormat = ImportFormats.Cashflow
                    };
            if (activity.HasErrors()) return activity.Finish();

            var allArgs = await GetAllArgsAsync(primaryArgs, options.TargetDataSource, ImportFormats.Cashflow);
            if (!dataSet.Tables.Contains(primaryArgs.ImportFormat))
                ApplicationMessage.Log(Error.TableNotFound, primaryArgs.ImportFormat);
            await DataNodeFactoryAsync(dataSet, ImportFormats.Cashflow, primaryArgs, options.TargetDataSource);
            if (activity.HasErrors()) return activity.Finish();

            var workspace = work.CreateNew();
            var log = await ParseCashflowsToWorkspaceAsync(dataSet, primaryArgs, workspace, options.TargetDataSource, activity, import);
            if (log.Errors.Any()) return activity.Finish().Merge(log);

            var workspaceToCompute = work.CreateNew();
            workspaceToCompute.Initialize(x => x.FromSource(options.TargetDataSource));
            if (Debug.Enable)
            {
                if (primaryArgs.Scenario == null)
                    await workspace.DeleteAsync(await workspace.Query<RawVariable>()
                        .Where(rv => rv.Values.Sum(x => Math.Abs(x)) < Consts.Precision).ToArrayAsync());
                var partition =
                    (Guid) (await workspace.Partition.GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(
                        primaryArgs));
                await workspace.CommitToAsync<RawVariable, PartitionByReportingNodeAndPeriod>(workspaceToCompute,
                    partition, snapshot: true);
            }
            else
                foreach (var args in allArgs)
                {
                    log = log.Merge(await ComputeAsync(args, workspace, workspaceToCompute, args == primaryArgs, activity, scopes));
                    if (log.Errors.Any()) return activity.Finish().Merge(log);
                }


            await workspaceToCompute.CommitToTargetAsync(options.TargetDataSource);
            return activity.Finish().Merge(log);
        });
    }


    public static async Task<ActivityLog> ParseActualsToWorkspaceAsync(IDataSet dataSet, ImportArgs args, IWorkspace workspace, IDataSource targetDataSource, IActivityVariable activity, IImportVariable import)
{
    workspace.Reset(x => x.ResetInitializationRules().ResetCurrentPartitions());
    workspace.Initialize(x => x.FromSource(targetDataSource)
                               .DisableInitialization<RawVariable>()
                               .DisableInitialization<IfrsVariable>());
    
    activity.Start();
    var parsingStorage = new ParsingStorage(args, targetDataSource, workspace);
    await parsingStorage.InitializeAsync();
    if(activity.HasErrors()) return activity.Finish();

    //var hasAccidentYearColumn = dataSet.Tables[ImportFormats.Actual].Columns.Any(x => x.ColumnName == nameof(IfrsVariable.AccidentYear));

    var importLog = await import.FromDataSet(dataSet)
        .WithType<IfrsVariable> ( (dataset, datarow) => {
            var dataNode = datarow.Field<string>(nameof(DataNode));
            if(!parsingStorage.DataNodeDataBySystemName.TryGetValue(dataNode, out var dataNodeData)) {
                ApplicationMessage.Log(Error.InvalidDataNode, dataNode);
                return null;
            }
            
            (string AmountType, string EstimateType) valueType = datarow.ParseAmountAndEstimateType(ImportFormats.Actual, parsingStorage.DimensionsWithExternalId, parsingStorage.EstimateType, parsingStorage.AmountType);
            var isStdActual = valueType.EstimateType == EstimateTypes.A;

            var aocType = datarow.Field<string>(nameof(IfrsVariable.AocType));
            if((!isStdActual && aocType != AocTypes.CF && aocType != AocTypes.WO) || (isStdActual && aocType != AocTypes.CF) ) {
                ApplicationMessage.Log(Error.AocTypeNotValid, aocType);
                return null;
            }

            var currentPeriodValue = ImportCalculationExtensions.GetSign(ImportFormats.Actual, 
                                (aocType, valueType.AmountType, valueType.EstimateType, dataNodeData.IsReinsurance), 
                                parsingStorage.HierarchyCache) * datarow.Field<string>("Value").CheckStringForExponentialAndConvertToDouble();
            
            int? accidentYear = dataset.ParseAccidentYear(datarow, ImportFormats.Actual);
            /*if(hasAccidentYearColumn && datarow.Field<string>(nameof(RawVariable.AccidentYear)) != null) {
                if(!Int32.TryParse(datarow.Field<string>(nameof(RawVariable.AccidentYear)), out var parsedAccidentYear)) {    
                    ApplicationMessage.Log(Error.AccidentYearTypeNotValid, datarow.Field<string>(nameof(RawVariable.AccidentYear))); return null;
                }
                else accidentYear = (int?)parsedAccidentYear;
            } */
            
            var item = new IfrsVariable {
                DataNode = dataNode,
                AocType = aocType,
                Novelty = Novelties.C,
                AccidentYear = accidentYear,
                AmountType = valueType.AmountType,
                EstimateType = valueType.EstimateType,
                Partition = parsingStorage.TargetPartitionByReportingNodeAndPeriod.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(currentPeriodValue)
            };
            return item;
        }, ImportFormats.Actual
    ).WithTarget(workspace).ExecuteAsync();
    
    await workspace.ValidateForDataNodeStateActiveAsync<IfrsVariable>(parsingStorage.DataNodeDataBySystemName);
    return activity.Finish().Merge(importLog);
}

    public static async Task DefineActualFormat(IImportVariable import, IActivityVariable activity, IWorkspaceVariable work, IScopeFactory scopes)
    {
        import.DefineFormat(ImportFormats.Actual, async (options, dataSet) =>
        {
            activity.Start();
            var primaryArgs =
                await GetArgsAndCommitPartitionAsync<PartitionByReportingNodeAndPeriod>(dataSet,
                        options.TargetDataSource) with
                    {
                        ImportFormat = ImportFormats.Actual
                    };
            if (activity.HasErrors()) return activity.Finish();

            var allArgs = await GetAllArgsAsync(primaryArgs, options.TargetDataSource, ImportFormats.Actual);
            if (!dataSet.Tables.Contains(primaryArgs.ImportFormat))
                ApplicationMessage.Log(Error.TableNotFound, primaryArgs.ImportFormat);
            await DataNodeFactoryAsync(dataSet, ImportFormats.Actual, primaryArgs, options.TargetDataSource);
            if (activity.HasErrors()) return activity.Finish();

            var workspace = work.CreateNew();
            var log = await ParseActualsToWorkspaceAsync(dataSet, primaryArgs, workspace, options.TargetDataSource, activity, import);
            if (log.Errors.Any()) return activity.Finish().Merge(log);

            var workspaceToCompute = work.CreateNew();
            workspaceToCompute.Initialize(x => x.FromSource(options.TargetDataSource));

            if (Debug.Enable)
            {
                var partition =
                    (Guid) (await workspace.Partition.GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(
                        primaryArgs));
                await workspace.CommitToAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(workspaceToCompute,
                    partition, snapshot: true);
            }
            else
                foreach (var args in allArgs)
                {
                    log = log.Merge(await ComputeAsync(args, workspace, workspaceToCompute, false, activity, scopes));
                    if (log.Errors.Any()) return activity.Finish().Merge(log);
                }

            await workspaceToCompute.CommitToTargetAsync(options.TargetDataSource);
            return activity.Finish().Merge(log);
        });
    }



    public static async Task<ActivityLog> ParseSimpleValueToWorkspaceAsync(IDataSet dataSet, ImportArgs args, IWorkspace workspace, IDataSource targetDataSource, IActivityVariable activity, IImportVariable import)
{
    workspace.Reset(x => x.ResetInitializationRules().ResetCurrentPartitions());
    workspace.Initialize(x => x.FromSource(targetDataSource)
                               .DisableInitialization<RawVariable>()
                               .DisableInitialization<IfrsVariable>());
    
    activity.Start();
    var importFormat = args.ImportFormat;
    var parsingStorage = new ParsingStorage(args, targetDataSource, workspace);
    await parsingStorage.InitializeAsync();
    if(activity.HasErrors()) return activity.Finish(); 

    //var hasAccidentYearColumn = dataSet.Tables[importFormat].Columns.Any(x => x.ColumnName == nameof(IfrsVariable.AccidentYear));

    var importLog = await import.FromDataSet(dataSet)
        .WithType<IfrsVariable> ( (dataset, datarow) => {
            var dataNode = parsingStorage.ValidateDataNode(datarow.Field<string>(nameof(DataNode)),importFormat);
            var amountType = parsingStorage.ValidateAmountType(datarow.Field<string>(nameof(IfrsVariable.AmountType)));
            var estimateType = parsingStorage.ValidateEstimateType(datarow.Field<string>(nameof(IfrsVariable.EstimateType)), dataNode); //TODO LIC/LRC dependence
            var aocStep = importFormat == ImportFormats.SimpleValue 
                                    ? parsingStorage.ValidateAocStep(new AocStep (datarow.Field<string>(nameof(IfrsVariable.AocType)), 
                                                                                  datarow.Field<string>(nameof(IfrsVariable.Novelty))))
                                    : new AocStep(AocTypes.BOP, Novelties.I);
            var economicBasis = importFormat == ImportFormats.SimpleValue 
                                    ? datarow.Field<string>(nameof(IfrsVariable.EconomicBasis)) 
                                    : null;
            
            parsingStorage.ValidateEstimateTypeAndAmountType(estimateType, amountType);
            
            var currentPeriodValue = ImportCalculationExtensions.GetSign(importFormat, 
                                        (aocStep.AocType, amountType, estimateType, parsingStorage.IsDataNodeReinsurance(dataNode)), 
                                        parsingStorage.HierarchyCache) * datarow.Field<string>("Value")
                                    .CheckStringForExponentialAndConvertToDouble();

            int? accidentYear = dataset.ParseAccidentYear(datarow, importFormat);
            /*if(hasAccidentYearColumn && datarow.Field<string>(nameof(RawVariable.AccidentYear)) != null) {
                if(!Int32.TryParse(datarow.Field<string>(nameof(RawVariable.AccidentYear)), out var parsedAccidentYear)) {    
                    ApplicationMessage.Log(Error.AccidentYearTypeNotValid, datarow.Field<string>(nameof(RawVariable.AccidentYear))); return null;
                }
                else accidentYear = (int?)parsedAccidentYear;
            } */

            var iv = new IfrsVariable {
                DataNode = dataNode,
                AocType = aocStep.AocType,
                Novelty = aocStep.Novelty,
                AccidentYear = accidentYear,
                AmountType = amountType,
                EstimateType = estimateType,
                EconomicBasis = economicBasis,
                Partition = parsingStorage.TargetPartitionByReportingNodeAndPeriod.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(currentPeriodValue)
            };
            return iv;
        }, importFormat // This should indicate the table name, not the input format
    ).WithTarget(workspace).ExecuteAsync();
    
    // Checking if there are inconsistencies in the TechnicalMarginEstimateTypes --> double entries in the steps where we expect to have unique values
    var invalidVariables = await workspace.Query<IfrsVariable>()
                            .Where(iv => parsingStorage.TechnicalMarginEstimateTypes.Contains(iv.EstimateType))
                            .Where(iv => ImportCalculationExtensions.GetAocTypeWithoutCsmSwitch().Contains(iv.AocType))
                            .GroupBy(iv => new {iv.DataNode, iv.AocType, iv.Novelty})
                            .Where(g => g.Count() > 1)
                            .Select(g => g.Key)
                            .ToArrayAsync();
    
    foreach (var iv in invalidVariables)
        ApplicationMessage.Log(Error.MultipleTechnicalMarginOpening, $"{iv.DataNode},{iv.AocType},{iv.Novelty}");
    
    await workspace.ValidateForDataNodeStateActiveAsync<IfrsVariable>(parsingStorage.DataNodeDataBySystemName);
    return activity.Finish().Merge(importLog);
}

    public static async Task DefineSimpleValueFormat(IImportVariable import, IActivityVariable activity, IWorkspaceVariable work)
    {
        import.DefineFormat(ImportFormats.SimpleValue, async (options, dataSet) =>
        {
            activity.Start();
            var args =
                await GetArgsAndCommitPartitionAsync<PartitionByReportingNodeAndPeriod>(dataSet,
                        options.TargetDataSource) with
                    {
                        ImportFormat = ImportFormats.SimpleValue
                    };
            if (!dataSet.Tables.Contains(args.ImportFormat))
                ApplicationMessage.Log(Error.TableNotFound, args.ImportFormat);
            if (activity.HasErrors()) return activity.Finish();
            await DataNodeFactoryAsync(dataSet, ImportFormats.SimpleValue, args, options.TargetDataSource);
            if (activity.HasErrors()) return activity.Finish();

            var workspace = work.CreateNew();
            var parsingLog = await ParseSimpleValueToWorkspaceAsync(dataSet, args, workspace, options.TargetDataSource, activity, import);
            if (parsingLog.Errors.Any()) return activity.Finish().Merge(parsingLog);

            workspace.Query<IfrsVariable>().Select(v => new {v.DataNode, v.AccidentYear}).Distinct();

            var targetDataNodes = workspace.Query<IfrsVariable>().Select(v => v.DataNode).Distinct().ToArray();
            await workspace.CommitToAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(
                options.TargetDataSource,
                (Guid) (await DataSource.Partition.GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(args)),
                snapshot: true, filter: x => targetDataNodes.Contains(x.DataNode));
            return activity.Finish().Merge(parsingLog);
        });
    }

    public static async Task DefineOpeningFormat(IImportVariable import, IActivityVariable activity, IWorkspaceVariable work, IScopeFactory scopes)
    {
        import.DefineFormat(ImportFormats.Opening, async (options, dataSet) =>
        {
            activity.Start();
            var primaryArgs =
                await GetArgsAndCommitPartitionAsync<PartitionByReportingNodeAndPeriod>(dataSet,
                        options.TargetDataSource) with
                    {
                        ImportFormat = ImportFormats.Opening
                    };
            if (primaryArgs.Scenario != default(string)) ApplicationMessage.Log(Error.NoScenarioOpening);
            if (!dataSet.Tables.Contains(primaryArgs.ImportFormat))
                ApplicationMessage.Log(Error.TableNotFound, primaryArgs.ImportFormat);
            if (activity.HasErrors()) return activity.Finish();

            var allArgs = await GetAllArgsAsync(primaryArgs, options.TargetDataSource, ImportFormats.Opening);
            await DataNodeFactoryAsync(dataSet, ImportFormats.Opening, primaryArgs, options.TargetDataSource);
            if (activity.HasErrors()) return activity.Finish();

            var workspace = work.CreateNew();
            var log = await ParseSimpleValueToWorkspaceAsync(dataSet, primaryArgs, workspace, options.TargetDataSource,
                activity, import);
            if (log.Errors.Any()) return activity.Finish().Merge(log);

            var workspaceToCompute = work.CreateNew();
            workspaceToCompute.Initialize(x => x.FromSource(options.TargetDataSource));

            if (Debug.Enable)
            {
                var partition =
                    (Guid) (await workspace.Partition.GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(
                        primaryArgs));
                await workspace.CommitToAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(workspaceToCompute,
                    partition,
                    snapshot: true);
            }
            else
                foreach (var args in allArgs)
                {
                    log = log.Merge(await ComputeAsync(args, workspace, workspaceToCompute, false, activity, scopes));
                    if (log.Errors.Any()) return activity.Finish().Merge(log);
                }

            await workspaceToCompute.CommitToTargetAsync(options.TargetDataSource, x => x.SnapshotMode<IfrsVariable>());
            return activity.Finish().Merge(log);

        });
    }
}





