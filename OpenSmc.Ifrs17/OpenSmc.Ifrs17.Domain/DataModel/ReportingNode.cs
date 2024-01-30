using System.ComponentModel.DataAnnotations;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record ReportingNode : KeyedDimension, IHierarchicalDimension
{
    [Dimension(typeof(ReportingNode))] public string? Parent { get; init; }

    [Required]
    [Dimension(typeof(Currency))]
    public virtual string? Currency { get; init; }
}