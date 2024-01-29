using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Scopes.Api;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public abstract record KeyedOrderedDimension : KeyedDimension, IOrdered
{
    [NotVisible] public int Order { get; init; }
}