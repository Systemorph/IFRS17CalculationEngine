using System.ComponentModel.DataAnnotations;
using OpenSmc.Domain.Abstractions.Attributes;

namespace OpenSmc.Ifrs17.DataTypes.DataModel;

public record InterDataNodeParameter : DataNodeParameter
{
    [Required]
    [IdentityProperty]
    [Dimension(typeof(GroupOfContract))]
    [Display(Order = 10)]
    public string? LinkedDataNode { get; init; }

    [Range(0, 1, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [Display(Order = 20)]
    public double ReinsuranceCoverage { get; init; }
}