using OpenSmc.Domain.Abstractions;

namespace OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

public record VariableType : KeyedOrderedDimension, IHierarchicalDimension
{
    public string Parent { get; init; }
}