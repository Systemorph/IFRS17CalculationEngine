using System.ComponentModel.DataAnnotations;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel;

public record PartitionByReportingNodeAndPeriod : IfrsPartition
{
    [Dimension(typeof(int), nameof(Year))]
    [IdentityProperty]
    [Display(Order = 20)]
    public int Year { get; init; }

    [Dimension(typeof(int), nameof(Month))]
    [IdentityProperty]
    [Display(Order = 30)]
    public int Month { get; init; }

    [Dimension(typeof(Scenario))]
    [IdentityProperty]
    [Display(Order = 40)]
    public string? Scenario { get; init; }
}