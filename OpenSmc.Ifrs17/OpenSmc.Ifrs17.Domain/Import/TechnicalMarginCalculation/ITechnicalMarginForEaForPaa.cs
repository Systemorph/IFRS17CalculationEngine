using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;
using OpenSmc.Ifrs17.Domain.Import.WrittenActualCalculation;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginForEaForPaa : ITechnicalMarginForEa
{
    double ITechnicalMarginForEa.Deferrable => GetScope<IDiscountedDeferrable>(Identity with { AocType = AocTypes.AM, Novelty = Novelties.C }).Value -
                                               GetStorage().GetDeferrableExpenses().Sum(d => GetScope<IWrittenActual>((Identity with { AocType = ReferenceAocType, Novelty = Novelties.C }, d, EstimateTypes.A, (int?)null)).Value);
}