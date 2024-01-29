using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;

public interface IDeferrableForBop : IDiscountedDeferrable
{
    //loop over amountTypes within deferrals to get bops
    double IDiscountedDeferrable.Value => GetStorage().GetValue(Identity, null, EstimateTypes.DA, null, Identity.ProjectionPeriod);
}