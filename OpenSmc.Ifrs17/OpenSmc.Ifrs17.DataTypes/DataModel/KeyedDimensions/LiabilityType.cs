using OpenSmc.Domain.Abstractions;
using OpenSmc.Domain.Abstractions.Attributes;


namespace OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

public record LiabilityType : KeyedOrderedDimension, IHierarchicalDimension
{
    [Dimension(typeof(LiabilityType))] public string Parent { get; init; }
}