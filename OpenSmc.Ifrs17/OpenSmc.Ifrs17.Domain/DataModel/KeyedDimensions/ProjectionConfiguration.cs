using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

public record ProjectionConfiguration : KeyedOrderedDimension
{
    [IdentityProperty] public int Shift { get; init; }
    [IdentityProperty] public int TimeStep { get; init; }
}