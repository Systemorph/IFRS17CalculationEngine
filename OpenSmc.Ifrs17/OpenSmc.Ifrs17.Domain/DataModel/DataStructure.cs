#r "nuget:Systemorph.Activities,1.6.5"
#r "nuget:Systemorph.Arithmetics,1.6.5"
#r "nuget:Systemorph.Workspace,1.6.4"
#r "nuget:Systemorph.InteractiveObjects,1.6.5"
//#r "nuget:Systemorph.SharePoint,1.6.5"
//#r "nuget:Systemorph.OneDrive,1.6.5"
#r "nuget:Systemorph.Scopes,1.6.5"
#r "nuget:Systemorph.Import,1.6.7"
#r "nuget:Systemorph.Test,1.6.5"
#r "nuget:Systemorph.Export,1.6.7"
#r "nuget:Systemorph.DataSetReader,1.6.6"
#r "nuget:Systemorph.DataSource,1.6.4"
#r "nuget:Systemorph.DataSource.Conversions,1.6.4"
#r "nuget:Systemorph.Reporting,1.6.5"
#r "nuget:Systemorph.Charting,1.6.5"
#r "nuget:Systemorph.SchemaMigrations,1.6.4"


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Systemorph.Vertex.Grid.Model;
using Systemorph.Vertex.Workspace;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Import;
using static Systemorph.Vertex.Arithmetics.ArithmeticOperations;


#!import "../Constants/Enums"
#!import "../Constants/Consts"
#!import "../Constants/Validations"


public interface IKeyed
{   
    public Guid Id { get; init; }
}


public interface IPartition : IKeyed {}


public interface IPartitioned
{
    public Guid Partition { get; init; }
}


public interface IWithYearAndMonth
{
    public int Year { get; init; }
    
    public int Month { get; init; }
}


public interface IWithYearMonthAndScenario : IWithYearAndMonth
{
    public string Scenario { get; init; }
}


public abstract record KeyedRecord : IKeyed {
    [Key]
    [NotVisible]     
    public Guid Id { get; init; }
}


public abstract record KeyedDimension : INamed {
    [Key]
    [IdentityProperty]
    [StringLength(50)]
    public string SystemName { get; init; }
    
    [NotVisible]
    public string DisplayName { get; init; }
}


public abstract record KeyedOrderedDimension : KeyedDimension, IOrdered {
    [NotVisible]
    public int Order { get; init; }
}


public abstract record KeyedOrderedDimensionWithExternalId : KeyedOrderedDimension {
    [Conversion(typeof(JsonConverter<string[]>))]
    public string[] ExternalId { get; init; }
}


public record HierarchicalDimensionWithLevel(string SystemName, string DisplayName, string Parent, int Level) : IHierarchicalDimension;


public record AmountType : KeyedOrderedDimensionWithExternalId, IHierarchicalDimension
{
    [Dimension(typeof(AmountType))]
    public string Parent { get; init; }
    
    [Dimension(typeof(PeriodType))]
    public PeriodType PeriodType { get; init; }
}


public record DeferrableAmountType : AmountType {}


public record RiskDriver : KeyedOrderedDimension, IHierarchicalDimension
{
    [Dimension(typeof(RiskDriver))]
    public string Parent { get; init; }
}


public record EstimateType : KeyedOrderedDimensionWithExternalId
{
    public InputSource InputSource { get; init; }
    
    public StructureType StructureType { get; init; }

    [Dimension(typeof(PeriodType))]
    public PeriodType PeriodType { get; init; }
}


public record Novelty : KeyedOrderedDimension {}


public record VariableType : KeyedOrderedDimension, IHierarchicalDimension
{    
    public string Parent { get; init; }
}


public record AocType : VariableType
{    
    [Dimension(typeof(AocType))]
    public string Parent { get; init; }
}


public record AocStep(string AocType, string Novelty){}


public record PnlVariableType : VariableType {}


public record BsVariableType : VariableType {}


public record AccountingVariableType : VariableType {}


public record Scenario : KeyedDimension {}


public record LineOfBusiness : KeyedOrderedDimension, IHierarchicalDimension
{
    [Dimension(typeof(LineOfBusiness))]
    public string Parent { get; init; }
}


public record Currency : KeyedDimension {}


public record EconomicBasis : KeyedDimension {}


public record ValuationApproach : KeyedDimension {}


public record LiabilityType : KeyedOrderedDimension, IHierarchicalDimension
{
    [Dimension(typeof(LiabilityType))]
    public string Parent { get; init; }
}


public record OciType : KeyedDimension {}


public record Profitability : KeyedDimension {}


public record Partner : KeyedDimension {}


public record CreditRiskRating : KeyedDimension {}


public record ReportingNode : KeyedDimension, IHierarchicalDimension 
{
    [Dimension(typeof(ReportingNode))]
    public string Parent { get; init; }
    
    [Required]
    [Dimension(typeof(Currency))]
    public virtual string Currency { get; init; }
}


public record ProjectionConfiguration : KeyedOrderedDimension
{
    [IdentityProperty]
    public int Shift { get; init; }
    [IdentityProperty]
    public int TimeStep { get; init; }
}


public record AocConfiguration : KeyedRecord, IWithYearAndMonth, IOrdered
{
    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Year))]
    [Range(1900, 2100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Year { get; init; }
    
    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Month))]
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Month { get; init; }
    
    [IdentityProperty]
    [Dimension(typeof(AocType))]
    public string AocType { get; init; }
    
    [IdentityProperty]
    [Dimension(typeof(Novelty))]
    public string Novelty { get; init; }
    
    [Dimension(typeof(DataType))]
    public DataType DataType { get; init; }
    
    [Dimension(typeof(StructureType))]
    public StructureType StructureType { get; init; }

    [Dimension(typeof(InputSource))]
    public InputSource InputSource { get; init; }
    
    [Dimension(typeof(FxPeriod))]
    public FxPeriod FxPeriod { get; init; }
    
    [Dimension(typeof(PeriodType), nameof(YcPeriod))]
    public PeriodType YcPeriod { get; init; }
    
    [Dimension(typeof(PeriodType), nameof(CdrPeriod))]
    public PeriodType CdrPeriod { get; init; }
    
    [Dimension(typeof(ValuationPeriod))]
    public ValuationPeriod ValuationPeriod { get; init; }
    
    [Dimension(typeof(PeriodType), nameof(RcPeriod))]
    public PeriodType RcPeriod { get; init; }
    
    [NotVisible]
    public int Order { get; init; }
}


public record ExchangeRate : KeyedRecord, IWithYearMonthAndScenario
{    
    [Required]
    [IdentityProperty]
    [Dimension(typeof(Currency))]
    public string Currency { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Year))]
    [Range(1900, 2100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Year { get; init; }
    
    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Month))]
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Month { get; init; }

    [IdentityProperty]
    [Required]
    public FxType FxType { get; init; }

    public double FxToGroupCurrency { get; init; }

    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }
}


public record CreditDefaultRate : KeyedRecord, IWithYearMonthAndScenario
{    
    [Required]
    [IdentityProperty]
    [Dimension(typeof(CreditRiskRating))]
    public string CreditRiskRating { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Year))]
    [Range(1900, 2100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Year { get; init; }
    
    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Month))]
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Month { get; init; }
    
    [Conversion(typeof(PrimitiveArrayConverter))]
    public double[] Values { get; init; }

    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }
}


public record YieldCurve : KeyedRecord, IWithYearMonthAndScenario
{    
    [Required]
    [IdentityProperty]
    [Dimension(typeof(Currency))]
    public string Currency { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Year))]
    [Range(1900, 2100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Year { get; init; }
    
    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Month))]
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Month { get; init; }

    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }

    [IdentityProperty]
    public string Name { get; init; }
    
    [Conversion(typeof(PrimitiveArrayConverter))]
    public double[] Values { get; init; }
}


public record PartnerRating : KeyedRecord, IWithYearMonthAndScenario
{    
    [Required]
    [IdentityProperty]
    [Dimension(typeof(Partner))]
    public string Partner { get; init; }

    [Required]
    [Dimension(typeof(CreditRiskRating))]
    public string CreditRiskRating { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Year))]
    [Range(1900, 2100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Year { get; init; }
    
    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Month))]
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Month { get; init; }

    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }
}


public abstract record IfrsPartition : IPartition {
    [Key]
    [PartitionId]
    public Guid Id { get; init; }

    [Required]
    [Dimension(typeof(ReportingNode))]
    [IdentityProperty]
    [Display(Order = 10)]
    public string ReportingNode { get; init; }
}


public record PartitionByReportingNode : IfrsPartition {}


public record PartitionByReportingNodeAndPeriod : IfrsPartition {
    [Dimension(typeof(int), nameof(Year))]
    [IdentityProperty]
    [Display(Order = 20)]
    public int Year { get; init; }

    [Dimension(typeof(int), nameof(Month))]
    [IdentityProperty]
    [Display(Order = 30)]
    public int Month { get; init; }
    
    [Dimension(typeof(Scenario))]
    [IdentityProperty]
    [Display(Order = 40)]
    public string Scenario { get; init; }
}


public record DataNode : KeyedDimension, IPartitioned {
    [NotVisible]
    [PartitionKey(typeof(PartitionByReportingNode))]
    public Guid Partition { get; init; }
    
    [NotVisible]    
    [Dimension(typeof(Currency))]
    //[Immutable]
    public string ContractualCurrency { get; init; }
    
    [NotVisible]    
    [Dimension(typeof(Currency))]
    //[Immutable]
    public string FunctionalCurrency { get; init; }
    
    [NotVisible]    
    [Dimension(typeof(LineOfBusiness))]
    //[Immutable]
    public string LineOfBusiness { get; init; }
    
    [NotVisible]    
    [Dimension(typeof(ValuationApproach))]
    [Required]
    //[Immutable]
    public string ValuationApproach { get; init; }
    
    [NotVisible]    
    [Dimension(typeof(OciType))]
    //[Immutable]
    public string OciType { get; init; }
}


public record Portfolio : DataNode {}

public record InsurancePortfolio : Portfolio {}
public record ReinsurancePortfolio : Portfolio {}


public record GroupOfContract : DataNode {
    [NotVisible]    
    [Dimension(typeof(int), nameof(AnnualCohort))]
    //[Immutable]
    public int AnnualCohort { get; init; }
    
    [NotVisible]    
    [Dimension(typeof(LiabilityType))]
    //[Immutable]
    public string LiabilityType { get; init; }
    
    [NotVisible]    
    [Dimension(typeof(Profitability))]
    //[Immutable]
    public string Profitability { get; init; }
 
    [Required]
    [NotVisible]    
    [Dimension(typeof(Portfolio))]
    //[Immutable]
    public string Portfolio { get; init; }

    [NotVisible]
    //[Immutable]
    public string YieldCurveName { get; init; }
    
    public virtual string Partner { get; init; }
}


public record GroupOfInsuranceContract : GroupOfContract {
    [Required]
    [NotVisible]    
    [Display(Name = "InsurancePortfolio")]
    [Dimension(typeof(InsurancePortfolio))]
    //[Immutable]
    public string Portfolio { get => base.Portfolio; init => base.Portfolio = value; }
    
    // TODO: for the case of internal reinsurance the Partner would be the reporting node, hence not null.
    // If this is true we need the [Required] attribute here, add some validation at dataNode import 
    // and to add logic in the GetNonPerformanceRiskRate method in ImportStorage.
    [NotVisible]    
    [NotMapped]
    //[Immutable]
    public override string Partner => null;
}

public record GroupOfReinsuranceContract : GroupOfContract {
    [Required]
    [NotVisible]    
    [Display(Name = "ReinsurancePortfolio")]
    [Dimension(typeof(ReinsurancePortfolio))]
    //[Immutable]
    public string Portfolio { get => base.Portfolio; init => base.Portfolio = value; }
}


public record DataNodeState : KeyedRecord, IPartitioned, IWithYearMonthAndScenario
{
    [NotVisible]
    [PartitionKey(typeof(PartitionByReportingNode))]
    public Guid Partition { get; init; }
    
    [Required]
    [IdentityProperty]
    [Dimension(typeof(GroupOfContract))]
    public string DataNode { get; init; }
    
    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Year))]
    [Range(1900, 2100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Year { get; init; }
    
    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Month))]
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [DefaultValue(DefaultDataNodeActivationMonth)]
    public int Month { get; init; } = DefaultDataNodeActivationMonth;
    
    [Required]
    [DefaultValue(State.Active)]
    public State State { get; init; } = State.Active;

    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }
}


public record DataNodeParameter : KeyedRecord, IPartitioned, IWithYearMonthAndScenario
{
    [NotVisible]
    [PartitionKey(typeof(PartitionByReportingNode))]
    public Guid Partition { get; init; }
        
    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Year))]
    [Range(1900, 2100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Year { get; init; }
    
    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Month))]
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [DefaultValue(DefaultDataNodeActivationMonth)]
    public int Month { get; init; } = DefaultDataNodeActivationMonth;
        
    [Required]
    [IdentityProperty]
    [Dimension(typeof(GroupOfContract))]
    [Display(Order = 1)]
    public string DataNode { get; init; }

    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }
}


public record SingleDataNodeParameter : DataNodeParameter {
    [DefaultValue(DefaultPremiumExperienceAdjustmentFactor)]
    [Range(0, 1, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [Display(Order = 20)]
    public double PremiumAllocation { get; init; } = DefaultPremiumExperienceAdjustmentFactor;

    [Dimension(typeof(CashFlowPeriodicity))]
    [Display(Order = 30)]
    public CashFlowPeriodicity CashFlowPeriodicity { get; init; }

    [Dimension(typeof(InterpolationMethod))]
    [Display(Order = 40)]
    public InterpolationMethod InterpolationMethod { get; init; }

    [Dimension(typeof(EconomicBasis))]
    [Display(Order = 50)]
    public string EconomicBasisDriver {get; init;}

    [Conversion(typeof(PrimitiveArrayConverter))]
    [Display(Order = 60)]
    public double[] ReleasePattern {get; init;}

}

public record InterDataNodeParameter : DataNodeParameter {
    [Required]
    [IdentityProperty]
    [Dimension(typeof(GroupOfContract))]
    [Display(Order = 10)]
    public string LinkedDataNode { get; init; }
    
    [Range(0, 1, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [Display(Order = 20)]
    public double ReinsuranceCoverage { get; init; }
}


public record DataNodeData {
    public string DataNode { get; init; }
    
    //Portfolio
    public string ContractualCurrency { get; init; }
    public string FunctionalCurrency { get; init; }
    public string LineOfBusiness { get; init; }
    public string ValuationApproach { get; init; }
    public string OciType { get; init; }
    
    //GroupOfContract
    public string Portfolio { get; init; }
    public int AnnualCohort { get; init; }
    public string LiabilityType { get; init; }
    public string Profitability { get; init; }
    public string Partner { get; init; }
    public string YieldCurveName { get; init; }
    
    
    //DataNodeState
    public int Year { get; init; }
    public int Month { get; init; }
    public State State { get; init; }
    public State PreviousState { get; init; }
    
    public bool IsReinsurance { get; init; }

    public DataNodeData(){}

    public DataNodeData(GroupOfContract dn)
    {
        DataNode = dn.SystemName;
        ContractualCurrency  = dn.ContractualCurrency;
        FunctionalCurrency  = dn.FunctionalCurrency;
        LineOfBusiness  = dn.LineOfBusiness;
        ValuationApproach  = dn.ValuationApproach;
        OciType  = dn.OciType;
        Portfolio  = dn.Portfolio;
        AnnualCohort  = dn.AnnualCohort;
        LiabilityType  = dn.LiabilityType;
        Profitability  = dn.Profitability;
        Partner  = dn.Partner;
        IsReinsurance  = dn.GetType().Name == nameof(GroupOfReinsuranceContract);
        YieldCurveName = dn.YieldCurveName;
    }
}


public abstract record BaseVariableIdentity {
    [NotVisible]
    [Dimension(typeof(GroupOfContract))]
    [IdentityProperty]
    public string DataNode { get; init; }
    
    [NotVisible]    
    [Dimension(typeof(AocType))]
    [IdentityProperty]
    public string AocType { get; init; }
    
    [NotVisible]    
    [Dimension(typeof(Novelty))]
    [IdentityProperty]
    public string Novelty { get; init; }
}


public abstract record BaseDataRecord : BaseVariableIdentity, IKeyed, IPartitioned {
    [Key]
    [NotVisible]     
    public Guid Id { get; init; }
    
    [NotVisible]
    [PartitionKey(typeof(PartitionByReportingNodeAndPeriod))]
    public Guid Partition { get; init; }
    
    [Conversion(typeof(PrimitiveArrayConverter))]
    public double[] Values {get; set;}
    
    [NotVisible]    
    [Dimension(typeof(EstimateType))]
    [IdentityProperty]
    public string EstimateType { get; init; }
    
    [NotVisible]    
    [Dimension(typeof(AmountType))]
    [IdentityProperty]
    public string AmountType { get; init; }
    
    [NotVisible]    
    [Dimension(typeof(int),nameof(AccidentYear))]
    [IdentityProperty]
    public int? AccidentYear { get; init; }
}


public record RawVariable : BaseDataRecord {}


public record IfrsVariable : BaseDataRecord
{
    [NotVisible]    
    [Dimension(typeof(EconomicBasis))]
    [IdentityProperty]
    public string EconomicBasis { get; init; }
    
    public IfrsVariable (){}
}


public record ImportIdentity : BaseVariableIdentity {
      
    [NotVisible]
    public bool IsReinsurance { get; init; }
    
    [NotVisible]
    public string ValuationApproach { get; init; }

    [NotVisible]
    public string LiabilityType { get; init; }
    
    [NotVisible]
    public int ProjectionPeriod { get; init; }
    
    public AocStep AocStep => new AocStep(AocType, Novelty);
    
    public ImportScope ImportScope { get; init; }
    
    public ImportIdentity(RawVariable rv){
        DataNode = rv.DataNode;
        AocType = rv.AocType;
        Novelty = rv.Novelty;
    }
    
    public ImportIdentity(IfrsVariable iv){
        DataNode = iv.DataNode;
        AocType = iv.AocType;
        Novelty = iv.Novelty;
    }

    public ImportIdentity(){}
}


public record ReportVariable {

    [NotVisible]
    [Dimension(typeof(ReportingNode))]
    [IdentityProperty]
    public string ReportingNode { get; init; }
    
    [NotVisible]
    [Dimension(typeof(Scenario))]
    [IdentityProperty]
    public string Scenario { get; init; }

    [NotVisible]
    [Dimension(typeof(Currency))]
    [IdentityProperty]
    [AggregateBy]
    public string Currency { get; init; }
    
    [NotVisible]
    [Dimension(typeof(Currency), nameof(FunctionalCurrency))]
    [IdentityProperty]
    public string FunctionalCurrency { get; init; }
    
    [NotVisible]
    [Dimension(typeof(Currency), nameof(ContractualCurrency))]
    [IdentityProperty]
    public string ContractualCurrency { get; init; }
    
    [NotVisible]
    [Dimension(typeof(GroupOfContract))]
    [IdentityProperty]
    public string GroupOfContract { get; init; }
    
    [NotVisible]
    [Dimension(typeof(Portfolio))]
    [IdentityProperty]
    public string Portfolio { get; init; }
    
    [NotVisible]
    [Dimension(typeof(LineOfBusiness))]
    [IdentityProperty]
    public string LineOfBusiness { get; init; }
    
    [NotVisible]
    [Dimension(typeof(LiabilityType))]
    [IdentityProperty]
    public string LiabilityType { get; init; }
    
    [NotVisible]
    [Dimension(typeof(Profitability), nameof(InitialProfitability))]
    [IdentityProperty]
    public string InitialProfitability { get; init; }
    
    [NotVisible]
    [Dimension(typeof(ValuationApproach))]
    [IdentityProperty]
    public string ValuationApproach { get; init; }
    
    [NotVisible]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(AnnualCohort))]
    [IdentityProperty]
    public int AnnualCohort { get; init; }
    
    [NotVisible]
    [Dimension(typeof(OciType))]
    [IdentityProperty]
    public string OciType { get; init; }
    
    [NotVisible]
    [Dimension(typeof(Partner))]
    [IdentityProperty]
    public string Partner { get; init; }
        
    [NotVisible]
    [IdentityProperty]
    public bool IsReinsurance { get; init; }
    
    [NotVisible]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(AccidentYear))]
    [IdentityProperty]
    public int AccidentYear { get; init; }

    [NotVisible]
    [Dimension(typeof(ServicePeriod))]
    [IdentityProperty]
    public ServicePeriod ServicePeriod { get; init; }

    [NotVisible]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(ProjectionConfiguration), nameof(Projection))]
    [IdentityProperty]
    public string Projection { get; init;}
    
    [NotVisible]
    [Dimension(typeof(VariableType))]
    [IdentityProperty]
    public string VariableType { get; init; }
    
    [NotVisible]
    [Dimension(typeof(Novelty))]
    [IdentityProperty]
    public string Novelty { get; init; }
    
    [NotVisible]
    [Dimension(typeof(AmountType))]
    [IdentityProperty]
    public string AmountType { get; init; }
    
    [NotVisible]
    [Dimension(typeof(EstimateType))]
    [IdentityProperty]
    public string EstimateType { get; init; }
    
    [NotVisible]
    [Dimension(typeof(EconomicBasis))]
    [IdentityProperty]
    public string EconomicBasis { get; init; }
    
    public double Value { get; init; }
    
    public ReportVariable(){}
    public ReportVariable(ReportVariable rv){
        ReportingNode = rv.ReportingNode;
        Scenario = rv.Scenario;
        Currency = rv.Currency;
        FunctionalCurrency = rv.FunctionalCurrency;
        ContractualCurrency = rv.ContractualCurrency;
        GroupOfContract = rv.GroupOfContract;
        Portfolio = rv.Portfolio;
        LineOfBusiness = rv.LineOfBusiness;
        LiabilityType = rv.LiabilityType;
        InitialProfitability = rv.InitialProfitability;
        ValuationApproach = rv.ValuationApproach;
        AnnualCohort = rv.AnnualCohort;
        OciType = rv.OciType;
        Partner = rv.Partner;
        IsReinsurance = rv.IsReinsurance;
        AccidentYear = rv.AccidentYear;
        ServicePeriod = rv.ServicePeriod;
        Projection = rv.Projection;
        VariableType = rv.VariableType;
        Novelty = rv.Novelty;
        AmountType = rv.AmountType;
        EstimateType = rv.EstimateType;
        EconomicBasis = rv.EconomicBasis;
        Value = rv.Value;
    }
}


public record Args
{
    [Required]
    [IdentityProperty]
    [Dimension(typeof(ReportingNode))]
    public string ReportingNode { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Year))]
    [Range(1900, 2100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Year { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Month))]
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Month { get; init; } 
    
    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }

    [IdentityProperty]
    public Periodicity Periodicity{ get; init; }

    public Args(string reportingNode, int year, int month, Periodicity periodicity, string scenario)
    {
        ReportingNode = reportingNode;
        Year = year;
        Month = month;
        Periodicity = periodicity;
        Scenario = scenario;
    }
}


public record ImportArgs : Args
{
    public string ImportFormat { get; init; }
       
    public ImportArgs(string reportingNode, int year, int month, Periodicity periodicity, string scenario, string importFormat)
        : base(reportingNode, year, month, periodicity, scenario)
    {
        ImportFormat = importFormat;
    }
}


public record ReportArgs : Args
{
    public string HierarchyName { get; init; }
    
    public CurrencyType CurrencyType { get; init; }
    
    public string ReportName { get; init; } // this is the key to which data to load (like loading behavior). If null, loads everything
    
    public ReportArgs(string reportingNode, int year, int month, Periodicity periodicity, string scenario, string hierarchyName, CurrencyType currencyType)
        : base(reportingNode, year, month, periodicity, scenario)
    {
        CurrencyType = currencyType;
        HierarchyName = hierarchyName;
    }
}



