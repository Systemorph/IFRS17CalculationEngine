using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginAllocation;

public interface IAllocateTechnicalMarginForBopProjection : IAllocateTechnicalMargin
{
    double IAllocateTechnicalMargin.TechnicalMargin => GetScope<IAllocateTechnicalMargin>(Identity with { AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.ProjectionPeriod - 1 }).Value;
    bool IAllocateTechnicalMargin.HasSwitch => false;
}