using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

public record AocType : VariableType
{
    [Dimension(typeof(AocType))] public string Parent { get; init; }
}