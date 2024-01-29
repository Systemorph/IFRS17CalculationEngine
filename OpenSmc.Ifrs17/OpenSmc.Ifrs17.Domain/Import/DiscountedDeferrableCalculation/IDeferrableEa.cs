using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;
using OpenSmc.Ifrs17.Domain.Import.WrittenActualCalculation;

namespace OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;

public interface IDeferrableEa : IDiscountedDeferrable
{
    private string ReferenceAocType => GetScope<IReferenceAocStep>(Identity).Values.First().AocType;
    double IDiscountedDeferrable.Value => GetStorage().GetDeferrableExpenses().Sum(at =>
        GetStorage().GetNovelties(ReferenceAocType, StructureType.AocPresentValue)
            .Sum(n => GetScope<IPresentValue>((Identity with { AocType = ReferenceAocType, Novelty = n }, at, EstimateTypes.BE, (int?)null), o => o.WithContext(EconomicBasis)).Value) -
        GetScope<IWrittenActual>((Identity with { AocType = ReferenceAocType, Novelty = Novelties.C }, at, EstimateTypes.A, (int?)null)).Value);
}