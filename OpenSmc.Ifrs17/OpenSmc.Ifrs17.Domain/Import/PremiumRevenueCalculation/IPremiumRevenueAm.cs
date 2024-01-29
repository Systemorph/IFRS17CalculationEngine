using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;

public interface IPremiumRevenueAm : IPremiumRevenue
{
    private double AmortizationFactor => GetScope<IDiscountedAmortizationFactorForRevenues>(Identity, o => o.WithContext(EconomicBasis)).Value;
    private double AggregatedValue => GetScope<AggregatedIPremiumRevenue>(Identity).AggregatedValue;
    double IPremiumRevenue.Value => Math.Abs(AggregatedValue) > Consts.Precision ? -1d * AggregatedValue * AmortizationFactor : default;
}