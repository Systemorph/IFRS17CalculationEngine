using OpenSmc.Domain.Abstractions;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

public record AmountType : KeyedOrderedDimensionWithExternalId, IHierarchicalDimension
{
    [Dimension(typeof(AmountType))] public string Parent { get; init; }

    [Dimension(typeof(PeriodType))] public PeriodType PeriodType { get; init; }
}