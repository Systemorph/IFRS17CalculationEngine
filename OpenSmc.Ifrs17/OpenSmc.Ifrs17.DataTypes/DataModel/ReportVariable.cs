using OpenSmc.Arithmetics;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel;

public record ReportVariable
{
    [NotVisible]
    [Dimension(typeof(ReportingNode))]
    [IdentityProperty]
    public string ReportingNode { get; init; }

    [NotVisible]
    [Dimension(typeof(Scenario))]
    [IdentityProperty]
    public string? Scenario { get; init; }

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
    public string Projection { get; init; }

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
    public string? AmountType { get; init; }

    [NotVisible]
    [Dimension(typeof(EstimateType))]
    [IdentityProperty]
    public string EstimateType { get; init; }

    [NotVisible]
    [Dimension(typeof(EconomicBasis))]
    [IdentityProperty]
    public string? EconomicBasis { get; init; }

    public double Value { get; init; }

    public ReportVariable()
    {
    }

    public ReportVariable(ReportVariable rv)
    {
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