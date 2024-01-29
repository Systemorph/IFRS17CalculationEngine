using System.ComponentModel.DataAnnotations;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel;

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
    public string Scenario { get; init; }
}