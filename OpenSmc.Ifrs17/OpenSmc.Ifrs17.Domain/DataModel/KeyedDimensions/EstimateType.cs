using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

public record EstimateType : KeyedOrderedDimensionWithExternalId
{
    public InputSource InputSource { get; init; }

    public StructureType StructureType { get; init; }

    [Dimension(typeof(PeriodType))] public PeriodType PeriodType { get; init; }
}