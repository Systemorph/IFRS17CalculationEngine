using OpenSmc.Domain.Abstractions.Attributes;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

public record AocType : VariableType
{
    [Dimension(typeof(AocType))] public string Parent { get; init; }
}