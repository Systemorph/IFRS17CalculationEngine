using System.ComponentModel.DataAnnotations;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.DataModel.Interfaces;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel;

public abstract record IfrsPartition : IPartition
{
    [Key]
    //[PartitionId]
    public Guid Id { get; init; }

    [Required]
    [Dimension(typeof(ReportingNode))]
    [IdentityProperty]
    [Display(Order = 10)]
    public string? ReportingNode { get; init; }
}