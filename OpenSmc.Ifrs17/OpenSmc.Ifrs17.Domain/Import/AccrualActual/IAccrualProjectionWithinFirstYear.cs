namespace OpenSmc.Ifrs17.Domain.Import.AccrualActual;

public interface IAccrualProjectionWithinFirstYear : IAccrualActual
{
    double IAccrualActual.Value => 
        GetScope<IAccrualActual>((Identity.Id with { ProjectionPeriod = 0 }, 
            Identity.AmountType, Identity.EstimateType, Identity.AccidentYear)).Value;
}