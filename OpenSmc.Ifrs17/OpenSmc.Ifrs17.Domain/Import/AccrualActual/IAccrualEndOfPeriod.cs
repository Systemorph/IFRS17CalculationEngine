using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Import.AccrualActual;

public interface IAccrualEndOfPeriod : IAccrualActual
{
    double IAccrualActual.Value => GetScope<IPreviousAocSteps>((Identity.Id, StructureType.AocAccrual)).Values.Sum(aocStep =>
        GetScope<IAccrualActual>((Identity.Id with { AocType = aocStep.AocType, Novelty = aocStep.Novelty }, Identity.AmountType, Identity.EstimateType, Identity.AccidentYear)).Value);
}