using System.Linq.Expressions;
using OpenSmc.Collections;
using OpenSmc.Data;
using OpenSmc.Hierarchies;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.Args;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.Interfaces;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;


namespace OpenSmc.Ifrs17.ImportHub;

public class ParsingStorage
    {
        //private readonly IWorkspace Workspace;
        private readonly IWorkspace Workspace;
        private string ImportFormat { get; set; }
        public ((int Year, int Month) Period, string ReportingNode, string Scenario, CurrencyType CurrencyType) Args { get; private set; }

        //Hierarchy Cache
        public IHierarchicalDimensionCache HierarchicalDimensionCache { get; }

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

        private Dictionary<string, HashSet<string>> amountTypesByEstimateType => GetAmountTypesByEstimateType(HierarchicalDimensionCache);

        public HashSet<string> TechnicalMarginEstimateTypes => GetTechnicalMarginEstimateType();
        public Dictionary<Type, Dictionary<string, string>> DimensionsWithExternalId;

        public Dictionary<string, Dictionary<int, SingleDataNodeParameter>> SingleDataNodeParametersByGoc
        {
            get;
            private set;
        }

        // Partitions
        public PartitionByReportingNode TargetPartitionByReportingNode;
        public PartitionByReportingNodeAndPeriod TargetPartitionByReportingNodeAndPeriod;

        //Constructor
        public ParsingStorage(ImportArgs Args, IWorkspace Workspace, IWorkspace workspace)
        {
            this.Workspace = workspace;
            HierarchicalDimensionCache = workspace.ToHierarchicalDimensionCache();
        }

        // Initialize
        public Task Initialize()
        {
            //Partition Workspace and Workspace
            TargetPartitionByReportingNode = Workspace.GetData<PartitionByReportingNode>().Single(p => p.ReportingNode == Args.ReportingNode);

            //await Workspace.Partition.SetAsync<PartitionByReportingNode>(TargetPartitionByReportingNode.Id);
            //await Workspace.Partition.SetAsync<PartitionByReportingNode>(TargetPartitionByReportingNode.Id);

            if (ImportFormat == ImportFormats.Cashflow || ImportFormat == ImportFormats.Actual ||
                ImportFormat == ImportFormats.SimpleValue || ImportFormat == ImportFormats.Opening)
            {
                TargetPartitionByReportingNodeAndPeriod = Workspace.GetData<PartitionByReportingNodeAndPeriod>()
                    .Single(p => p.ReportingNode == Args.ReportingNode &&
                                          p.Year == Args.Period.Year&&
                                          p.Month == Args.Period.Month &&
                                          p.Scenario == Args.Scenario);


                //await Workspace.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(
                //    TargetPartitionByReportingNodeAndPeriod.Id);
                //await Workspace.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(
                //    TargetPartitionByReportingNodeAndPeriod.Id);
                //
                ////Clean up the workspace
                //await Workspace.DeleteAsync<RawVariable>(await Workspace.Query<RawVariable>().ToArrayAsync());
                //await Workspace.DeleteAsync<IfrsVariable>(await Workspace.Query<IfrsVariable>().ToArrayAsync());
            }

            var reportingNodes = Workspace.GetData<ReportingNode>().Where(x => x.SystemName == Args.ReportingNode);
            
            ReportingNode = reportingNodes.First();

            var aocConfigurationByAocStep = Workspace.GetData<AocConfiguration>().GroupBy(x => (x.AocType, x.Novelty),
                (k, v) => v.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).First()).ToList();
            MandatoryAocSteps = aocConfigurationByAocStep.Where(x => x.DataType == DataType.Mandatory)
                .Select(x => new AocStep(x.AocType, x.Novelty)).ToHashSet();
            AocTypeMap = ImportFormat switch
            {
                ImportFormats.Cashflow => aocConfigurationByAocStep.Where(x =>
                        x.InputSource.Contains(InputSource.Cashflow) &&
                        !new DataType[] { DataType.Calculated, DataType.CalculatedTelescopic }.Contains(x.DataType))
                    .GroupBy(x => new AocStep(x.AocType, x.Novelty), (k, v) => k).ToHashSet(),
                ImportFormats.Actual => aocConfigurationByAocStep.Where(x =>
                        x.InputSource.Contains(InputSource.Actual) &&
                        !new DataType[] { DataType.Calculated, DataType.CalculatedTelescopic }.Contains(x.DataType) &&
                        new AocStep(x.AocType, x.Novelty) != new AocStep(AocTypes.BOP, Novelties.I))
                    .GroupBy(x => new AocStep(x.AocType, x.Novelty), (k, v) => k).ToHashSet(),
                ImportFormats.Opening => aocConfigurationByAocStep
                    .Where(x => x.InputSource.Contains(InputSource.Opening) && x.DataType == DataType.Optional)
                    .GroupBy(x => new AocStep(x.AocType, x.Novelty), (k, v) => k).ToHashSet(),
                ImportFormats.SimpleValue => aocConfigurationByAocStep
                    .GroupBy(x => new AocStep(x.AocType, x.Novelty), (k, v) => k)
                    .Concat(Workspace.GetData<PnlVariableType>()
                        .Select(vt => new AocStep(vt.SystemName, null))).ToHashSet(),
                _ => Enumerable.Empty<AocStep>().ToHashSet(),
            };
            DataNodeDataBySystemName = ImportFormat == ImportFormats.Opening ? LoadDataNodes(Workspace, Args).Where(kvp => kvp.Value.Year == Args.Period.Year).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : await LoadDataNodesAsync(Workspace, Args);

            SingleDataNodeParametersByGoc = Workspace.LoadSingleDataNodeParameters(Args);

            // Dimensions
            EstimateType = Workspace.GetData<EstimateType>().ToDictionary(x => x.SystemName);
            AmountType = Workspace.GetData<AmountType>().Where(x => !(x is DeferrableAmountType)).ToDictionary(x => x.SystemName);
            amountTypes = Workspace.GetData<AmountType>().Select(at => at.SystemName).ToHashSet();
            economicBasis = Workspace.GetData<EconomicBasis>().Select(eb => eb.SystemName).ToHashSet();
            estimateTypes = ImportFormat switch
            {
                ImportFormats.SimpleValue => Workspace.GetData<EstimateType>().Select(et => et.SystemName).ToHashSet(),
                ImportFormats.Opening => Workspace.GetData<EstimateType>().Where(et => et.StructureType == StructureType.AoC)
                    .Where(et => et.InputSource.Contains(InputSource.Opening)) //This Contains overload cannot be used in DB, thus the ToArrayAsync()
                    .Select(et => et.SystemName).ToHashSet(),
                _ => Enumerable.Empty<string>().ToHashSet(),
            };


            // DimensionsWithExternalId
            DimensionsWithExternalId = new Dictionary<Type, Dictionary<string, string>>()
            {
                { typeof(AmountType), GetDimensionWithExternalIdDictionary<AmountType>() },
                { typeof(EstimateType), GetDimensionWithExternalIdDictionary<EstimateType>() }
            };

            HierarchicalDimensionCache.Initialize<AmountType>();
        }

        public Task<Dictionary<string, string>> GetDimensionWithExternalIdDictionary<T>()
            where T : KeyedOrderedDimension
        {
            var dict = new Dictionary<string, string>();
            var items = Workspace.GetData<T>().ToArray();
            foreach (var item in items)
            {
                dict.TryAdd(item.SystemName, item.SystemName);
                if (typeof(T).IsAssignableTo(typeof(KeyedOrderedDimensionWithExternalId)))
                {
                    var externalIds = (string[])(typeof(T)
                        .GetProperty(nameof(KeyedOrderedDimensionWithExternalId.ExternalId)).GetValue(item));
                    if (externalIds == null) continue;
                    foreach (var extId in externalIds)
                        dict.TryAdd(extId, item.SystemName);
                }
            }

            return dict;
        }

        // Getters
        public bool IsDataNodeReinsurance(string goc) => DataNodeDataBySystemName[goc].IsReinsurance;
        public bool IsValidDataNode(string goc) => DataNodeDataBySystemName.ContainsKey(goc);

        public CashFlowPeriodicity GetCashFlowPeriodicity(string goc)
        {
            if (!SingleDataNodeParametersByGoc.TryGetValue(goc, out var inner))
                return CashFlowPeriodicity.Monthly;
            return inner[CurrentPeriod].CashFlowPeriodicity;
        }

        public InterpolationMethod GetInterpolationMethod(string goc)
        {
            if (!SingleDataNodeParametersByGoc.TryGetValue(goc, out var inner))
                return InterpolationMethod.NotApplicable;
            return inner[CurrentPeriod].InterpolationMethod;
        }

        // Validations
        public string ValidateEstimateType(string et, string goc)
        {
            var allowedEstimateTypes = estimateTypes;
            if (DataNodeDataBySystemName.TryGetValue(goc, out var dataNodeData) &&
                dataNodeData.LiabilityType == LiabilityTypes.LIC)
                estimateTypes.ExceptWith(TechnicalMarginEstimateTypes);
            if (!allowedEstimateTypes.Contains(et))
                ApplicationMessage.Log(Error.EstimateTypeNotFound, et);
            return et;
        }

        public string ValidateAmountType(string at)
        {
            if (at != null && !amountTypes.Contains(at))
                ApplicationMessage.Log(Error.AmountTypeNotFound, at);
            return at;
        }

        public AocStep ValidateAocStep(AocStep aoc)
        {
            if (!AocTypeMap.Contains(aoc))
                ApplicationMessage.Log(Error.AocTypeMapNotFound, aoc.AocType, aoc.Novelty);
            return aoc;
        }

        public string ValidateDataNode(string goc, string importFormat)
        {
            if (!DataNodeDataBySystemName.ContainsKey(goc))
            {
                if (importFormat == ImportFormats.Opening)
                    ApplicationMessage.Log(Error.InvalidDataNodeForOpening, goc);
                else
                    ApplicationMessage.Log(Error.InvalidDataNode, goc);
            }

            return goc;
        }

        public void ValidateEstimateTypeAndAmountType(string estimateType, string amountType)
        {
            if (amountTypesByEstimateType.TryGetValue(estimateType, out var ats) && ats.Any() &&
                !ats.Contains(amountType))
                ApplicationMessage.Log(Error.InvalidAmountTypeEstimateType, estimateType, amountType);
        }

        public string ValidateEconomicBasisDriver(string eb, string goc)
        {
            if (string.IsNullOrEmpty(eb))
                return GetDefaultEconomicBasisDriver(DataNodeDataBySystemName[goc].ValuationApproach,
                    DataNodeDataBySystemName[goc].LiabilityType);
            if (!economicBasis.Contains(eb))
            {
                ApplicationMessage.Log(Error.InvalidEconomicBasisDriver, goc);
                return null;
            }

            return eb;
        }
       
}


