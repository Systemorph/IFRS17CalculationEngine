using OpenSmc.Ifrs17.Domain.Import.NominalCashflow;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;

public interface ICashflowAocStep : IPresentValue
{
    [NotVisible] double[] IPresentValue.Values => GetScope<INominalCashflow>(Identity).Values;
}