using OpenSmc.Domain.Abstractions;

namespace OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

public record VariableType : KeyedOrderedDimension, IHierarchicalDimension, Systemorph.Vertex.Api.IHierarchicalDimension
{
    public string Parent { get; init; }
}