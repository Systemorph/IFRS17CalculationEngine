using OpenSmc.Ifrs17.Domain.Constants;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginForCurrentBasis : ITechnicalMargin
{
    [NotVisible] string ITechnicalMargin.EconomicBasis => EconomicBases.C;
}