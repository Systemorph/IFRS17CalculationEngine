using OpenSmc.Ifrs17.Domain.Constants;
using Systemorph.Vertex.Api;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record AmountType : KeyedOrderedDimensionWithExternalId, IHierarchicalDimension
{
    [Dimension(typeof(AmountType))] public string Parent { get; init; }

    [Dimension(typeof(PeriodType))] public PeriodType PeriodType { get; init; }
}