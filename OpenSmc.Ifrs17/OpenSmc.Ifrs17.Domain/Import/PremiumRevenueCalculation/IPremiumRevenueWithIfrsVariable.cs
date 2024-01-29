namespace OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;

public interface IPremiumRevenueWithIfrsVariable : IPremiumRevenue
{
    double IPremiumRevenue.Value => GetStorage().GetValue(Identity, AmountType, EstimateType, EconomicBasis, null, Identity.ProjectionPeriod);
}