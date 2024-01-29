namespace OpenSmc.Ifrs17.Domain.Import.ExperienceAdjustmentForPremium;

public interface IDefaultValueIBeExperienceAdjustmentForPremium : IBeExperienceAdjustmentForPremium
{
    double IBeExperienceAdjustmentForPremium.Value => default;
}