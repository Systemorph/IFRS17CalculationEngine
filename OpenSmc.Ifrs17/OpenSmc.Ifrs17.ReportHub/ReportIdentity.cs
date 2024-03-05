using OpenSmc.Arithmetics;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.ReportHub;

[IdentityAggregationBehaviour(IdentityAggregationBehaviour.Aggregate)]
public record ReportIdentity
{

    [Dimension(typeof(int), nameof(Year))]
    public int Year { get; init; }

    [Dimension(typeof(int), nameof(Month))]
    public int Month { get; init; }

    [Dimension(typeof(ReportingNode))]
    public string ReportingNode { get; init; }

    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }

    [Dimension(typeof(Currency), nameof(ContractualCurrency))]
    public string ContractualCurrency { get; init; }

    [Dimension(typeof(Currency), nameof(FunctionalCurrency))]
    public string FunctionalCurrency { get; init; }

    [NotAggregated]
    [Dimension(typeof(ProjectionConfiguration), nameof(Projection))]
    public string Projection { get; init; }

    [Dimension(typeof(LiabilityType))]
    public string LiabilityType { get; init; }

    [Dimension(typeof(ValuationApproach))]
    public string ValuationApproach { get; init; }

    public bool IsReinsurance { get; init; } //TODO use ReinsuranceType

    public bool IsOci { get; init; }
}