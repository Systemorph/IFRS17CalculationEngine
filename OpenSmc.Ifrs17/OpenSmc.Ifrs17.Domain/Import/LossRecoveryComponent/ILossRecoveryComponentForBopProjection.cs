using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.LossRecoveryComponent;

public interface ILossRecoveryComponentForBopProjection : ILossRecoveryComponent
{
    double ILossRecoveryComponent.Value => GetScope<ILossRecoveryComponent>(Identity with { AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.ProjectionPeriod - 1 }).Value;
}