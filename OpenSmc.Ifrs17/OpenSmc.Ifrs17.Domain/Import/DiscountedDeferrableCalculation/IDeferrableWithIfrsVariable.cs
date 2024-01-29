namespace OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;

public interface IDeferrableWithIfrsVariable : IDiscountedDeferrable
{
    double IDiscountedDeferrable.Value => GetStorage().GetValue(Identity, AmountType, EstimateType, EconomicBasis, null, Identity.ProjectionPeriod);
}