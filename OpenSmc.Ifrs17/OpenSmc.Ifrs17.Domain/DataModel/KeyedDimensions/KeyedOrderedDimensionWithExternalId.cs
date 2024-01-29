using Systemorph.Vertex.Persistence.EntityFramework.Conversions.Api;
using Systemorph.Vertex.Persistence.EntityFramework.Conversions.Converters;

namespace OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

public abstract record KeyedOrderedDimensionWithExternalId : KeyedOrderedDimension
{
    [Conversion(typeof(JsonConverter<string[]>))]
    public string[] ExternalId { get; init; }
}