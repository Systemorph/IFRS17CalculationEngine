using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.NominalDeferrableCalculation;

public interface IBoPDeferrableProjection : INominalDeferrable
{
    double INominalDeferrable.Value => GetScope<INominalDeferrable>((Identity.Id with { AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.Id.ProjectionPeriod - 1 }, Identity.MonthlyShift)).Value;
}