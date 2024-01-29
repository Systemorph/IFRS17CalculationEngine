using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;

namespace OpenSmc.Ifrs17.Domain.Import.ActualExperienceAdjustmentOnPremium;

public interface IActualExperienceAdjustmentOnPremiumForPaa : IActualExperienceAdjustmentOnPremium
{
    double IActualExperienceAdjustmentOnPremium.Value => GetScope<IPremiumRevenue>(Identity with { AocType = AocTypes.AM, Novelty = Novelties.C }).Value;
}