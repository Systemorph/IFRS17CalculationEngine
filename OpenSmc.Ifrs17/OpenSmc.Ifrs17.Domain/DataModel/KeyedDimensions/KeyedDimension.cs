using System.ComponentModel.DataAnnotations;
using Systemorph.Vertex.Api;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

public abstract record KeyedDimension : INamed
{
    [Key]
    [IdentityProperty]
    [StringLength(50)]
    public string SystemName { get; init; }

    [NotVisible] public string DisplayName { get; init; }
}