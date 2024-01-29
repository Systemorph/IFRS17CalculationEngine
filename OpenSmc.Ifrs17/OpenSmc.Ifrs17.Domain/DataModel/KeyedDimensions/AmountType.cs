using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using Systemorph.Vertex.Api;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

public record AmountType : KeyedOrderedDimensionWithExternalId, IHierarchicalDimension
{
    [Dimension(typeof(AmountType))] public string Parent { get; init; }

    [Dimension(typeof(PeriodType))] public PeriodType PeriodType { get; init; }
}