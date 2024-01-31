using System.ComponentModel.DataAnnotations;
using OpenSmc.Domain.Abstractions;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record ReportingNode : KeyedDimension, IHierarchicalDimension, Systemorph.Vertex.Api.IHierarchicalDimension
{
    [Dimension(typeof(ReportingNode))] public string? Parent { get; init; }

    [Required]
    [Dimension(typeof(Currency))]
    public virtual string? Currency { get; init; }
}