using System.ComponentModel.DataAnnotations;
using OpenSmc.Ifrs17.Domain.DataModel.Interfaces;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Partition;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public abstract record IfrsPartition : IPartition
{
    [Key][PartitionId] public Guid Id { get; init; }

    [Required]
    [Dimension(typeof(ReportingNode))]
    [IdentityProperty]
    [Display(Order = 10)]
    public string ReportingNode { get; init; }
}