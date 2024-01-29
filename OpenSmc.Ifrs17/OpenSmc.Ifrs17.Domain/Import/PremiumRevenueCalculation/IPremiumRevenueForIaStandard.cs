namespace OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;

public interface IPremiumRevenueForIaStandard : IPremiumRevenue, IInterestAccretionFactor
{
    private double AggregatedValue => GetScope<AggregatedIPremiumRevenue>(Identity).AggregatedValue;
    double IPremiumRevenue.Value => AggregatedValue * GetInterestAccretionFactor(EconomicBasis);
}