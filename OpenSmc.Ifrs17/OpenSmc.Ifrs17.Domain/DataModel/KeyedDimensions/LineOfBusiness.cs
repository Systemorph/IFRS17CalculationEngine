using OpenSmc.Domain.Abstractions;
using OpenSmc.Domain.Abstractions.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

public record LineOfBusiness : KeyedOrderedDimension, IHierarchicalDimension
{
    [Dimension(typeof(LineOfBusiness))] public string Parent { get; init; }
}