using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;

namespace OpenSmc.Ifrs17.Domain.Import.WrittenActualCalculation;

public interface IActualProjection : IWrittenActual
{
    double IWrittenActual.Value => GetStorage().GetValues(Identity.Id with { AocType = AocTypes.CL, Novelty = Novelties.C }, Identity.AmountType, EstimateTypes.PCE, Identity.AccidentYear).Any()
        ? GetScope<IActualFromPaymentPattern>(Identity).Value
        : GetStorage().GetNovelties(Identity.Id.AocType, StructureType.AocPresentValue)
            .Sum(novelty => GetScope<IPresentValue>((Identity.Id with { AocType = AocTypes.CF, Novelty = novelty }, Identity.AmountType, EstimateTypes.BE, Identity.AccidentYear), o => o.WithContext(EconomicBases.C)).Value);
}