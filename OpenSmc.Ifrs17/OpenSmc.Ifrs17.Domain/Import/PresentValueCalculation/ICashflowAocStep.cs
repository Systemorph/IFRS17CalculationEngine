using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Import.NominalCashflow;

namespace OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;

public interface ICashflowAocStep : IPresentValue
{
    [NotVisible] double[] IPresentValue.Values => GetScope<INominalCashflow>(Identity).Values;
}