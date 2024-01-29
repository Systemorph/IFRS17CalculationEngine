using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.LossRecoveryComponent;

public interface ILossRecoveryComponentForBop : ILossRecoveryComponent
{
    double ILossRecoveryComponent.Value => -1d * GetStorage().GetValue(Identity, null, EstimateTypes.LR, null, Identity.ProjectionPeriod);
}