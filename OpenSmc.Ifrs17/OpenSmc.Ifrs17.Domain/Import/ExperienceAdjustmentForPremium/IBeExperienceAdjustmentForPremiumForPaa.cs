using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;

namespace OpenSmc.Ifrs17.Domain.Import.ExperienceAdjustmentForPremium;

public interface IBeExperienceAdjustmentForPremiumForPaa : IBeExperienceAdjustmentForPremium
{
    double IBeExperienceAdjustmentForPremium.Value => GetScope<IPremiumRevenue>(Identity with { AocType = AocTypes.AM, Novelty = Novelties.C }).Value;
}