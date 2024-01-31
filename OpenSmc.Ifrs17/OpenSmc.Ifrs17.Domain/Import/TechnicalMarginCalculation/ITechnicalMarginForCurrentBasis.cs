using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginForCurrentBasis : ITechnicalMargin
{
    [NotVisible] string ITechnicalMargin.EconomicBasis => EconomicBases.C;
}