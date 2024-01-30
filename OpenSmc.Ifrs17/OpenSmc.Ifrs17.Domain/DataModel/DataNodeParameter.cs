using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using OpenSmc.Arithmetics;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel.Interfaces;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Partition;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record DataNodeParameter : KeyedRecord, IPartitioned, IWithYearMonthAndScenario
{
    [NotVisible]
    [PartitionKey(typeof(PartitionByReportingNode))]
    public Guid Partition { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Year))]
    [Range(1900, 2100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Year { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Month))]
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [DefaultValue(Consts.DefaultDataNodeActivationMonth)]
    public int Month { get; init; } = Consts.DefaultDataNodeActivationMonth;

    [Required]
    [IdentityProperty]
    [Dimension(typeof(GroupOfContract))]
    [Display(Order = 1)]
    public string DataNode { get; init; }

    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }
}