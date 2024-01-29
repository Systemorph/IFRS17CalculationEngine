using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;

public interface IEmptyValuesAocStep : IPresentValue
{
    [NotVisible] double[] IPresentValue.Values => Enumerable.Empty<double>().ToArray();
}