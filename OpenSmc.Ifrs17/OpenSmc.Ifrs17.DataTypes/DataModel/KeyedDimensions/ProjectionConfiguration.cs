using OpenSmc.Domain.Abstractions.Attributes;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

public record ProjectionConfiguration : KeyedOrderedDimension
{
    [IdentityProperty] public int Shift { get; init; }
    [IdentityProperty] public int TimeStep { get; init; }
}