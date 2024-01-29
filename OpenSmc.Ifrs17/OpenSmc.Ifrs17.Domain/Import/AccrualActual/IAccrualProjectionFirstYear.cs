using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.AccrualActual;

public interface IAccrualProjectionFirstYear : IAccrualActual
{
    private double SignMultiplier => Identity.Id.AocType == AocTypes.BOP ? 1d : -1d;
    double IAccrualActual.Value => SignMultiplier * GetScope<IAccrualActual>((Identity.Id with { AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.Id.ProjectionPeriod - 1 },
        Identity.AmountType, Identity.EstimateType, Identity.AccidentYear)).Value;
}