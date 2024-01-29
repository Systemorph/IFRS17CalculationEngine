using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;

public interface IPremiumRevenueForBop : IPremiumRevenue
{
    double IPremiumRevenue.Value => GetStorage().GetValue(Identity, AmountType, EstimateTypes.R, EconomicBasis, null, Identity.ProjectionPeriod);
}