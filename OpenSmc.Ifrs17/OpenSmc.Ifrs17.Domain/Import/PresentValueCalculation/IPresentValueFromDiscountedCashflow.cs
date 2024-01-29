using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;

public interface IPresentValueFromDiscountedCashflow : IPresentValue
{
    [NotVisible] double[] IPresentValue.Values => GetScope<IDiscountedCashflow>(Identity).Values;
}