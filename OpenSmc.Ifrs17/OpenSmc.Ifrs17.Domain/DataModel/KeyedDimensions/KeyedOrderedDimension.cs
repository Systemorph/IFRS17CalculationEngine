using OpenSmc.DataCubes;
using OpenSmc.Domain.Abstractions.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

public abstract record KeyedOrderedDimension : KeyedDimension, IOrdered
{
    [NotVisible] public int Order { get; init; }
}