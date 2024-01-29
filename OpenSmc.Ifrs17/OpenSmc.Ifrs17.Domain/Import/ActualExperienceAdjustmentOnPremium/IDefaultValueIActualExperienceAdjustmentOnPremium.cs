namespace OpenSmc.Ifrs17.Domain.Import.ActualExperienceAdjustmentOnPremium;

public interface IDefaultValueIActualExperienceAdjustmentOnPremium : IActualExperienceAdjustmentOnPremium
{
    double IActualExperienceAdjustmentOnPremium.Value => default;
}