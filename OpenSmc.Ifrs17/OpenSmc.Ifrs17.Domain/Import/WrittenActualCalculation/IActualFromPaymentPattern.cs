using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Import.WrittenActualCalculation;

public interface IActualFromPaymentPattern : IWrittenActual, IWithGetValueFromValues
{
    double IWrittenActual.Value => GetValueFromValues(
        GetStorage().GetValues(Identity.Id with { AocType = AocTypes.CL, Novelty = Novelties.C }, Identity.AmountType, EstimateTypes.PCE, Identity.AccidentYear),
        ValuationPeriod.Delta.ToString());
}