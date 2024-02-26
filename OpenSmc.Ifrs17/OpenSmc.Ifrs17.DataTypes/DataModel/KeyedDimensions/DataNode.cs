using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

public record DataNode : KeyedDimension
{

    [NotVisible] public string ReportingNode { get; init; } //This property is used to partition the data (PartitionByReportingNode).

    [NotVisible]
    [Dimension(typeof(Currency))]
    public string ContractualCurrency { get; init; }

    [NotVisible]
    [Dimension(typeof(Currency))]
    public string FunctionalCurrency { get; init; }

    [NotVisible]
    [Dimension(typeof(LineOfBusiness))]
    public string LineOfBusiness { get; init; }

    [NotVisible]
    [Dimension(typeof(ValuationApproach))]
    [Required]
    public string ValuationApproach { get; init; }

    [NotVisible]
    [Dimension(typeof(OciType))]
    public string OciType { get; init; }
}

public record Portfolio : DataNode
{
}
public record InsurancePortfolio : Portfolio
{
}

public record ReinsurancePortfolio : Portfolio
{
}

public record GroupOfContract : DataNode
{
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

public record GroupOfInsuranceContract : GroupOfContract
{
    [Required]
    [NotVisible]
    [Display(Name = "InsurancePortfolio")]
    [Dimension(typeof(InsurancePortfolio))]
    //[Immutable]
    public string Portfolio
    {
        get => base.Portfolio;
        init => base.Portfolio = value;
    }

    // TODO: for the case of internal reinsurance the Partner would be the reporting node, hence not null.
    // If this is true we need the [Required] attribute here, add some validation at dataNode import 
    // and to add logic in the GetNonPerformanceRiskRate method in ImportStorage.
    [NotVisible]
    [NotMapped]
    //[Immutable]
    public override string Partner => null;
}

public record GroupOfReinsuranceContract : GroupOfContract
{
    [Required]
    [NotVisible]
    [Display(Name = "ReinsurancePortfolio")]
    [Dimension(typeof(ReinsurancePortfolio))]
    //[Immutable]
    public string Portfolio
    {
        get => base.Portfolio;
        init => base.Portfolio = value;
    }
}