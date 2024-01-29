using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;

public interface IDeferrableAfterFirstYear : IDiscountedDeferrable
{
    double IDiscountedDeferrable.Value => GetScope<IDiscountedDeferrable>(Identity with { AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.ProjectionPeriod - 1 }).Value;
}