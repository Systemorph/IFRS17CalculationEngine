using System.ComponentModel.DataAnnotations;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.DataModel.Interfaces;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel;

public abstract record IfrsPartition : IPartition
{
    [Required]
    [Dimension(typeof(ReportingNode))]
    [IdentityProperty]
    [Key]
    [Display(Order = 10)]
    public string ReportingNode { get; init; }
}