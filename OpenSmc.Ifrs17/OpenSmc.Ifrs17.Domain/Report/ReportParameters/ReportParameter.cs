using System.ComponentModel.DataAnnotations;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Attributes.Arithmetics;

namespace OpenSmc.Ifrs17.Domain.Report.ReportParameters;

public abstract record ReportParameter
{
    [Display(Order = -100)]
    [IdentityProperty]
    [NotAggregated]
    [Dimension(typeof(int), nameof(Year))]
    public int Year { get; init; }

    [Display(Order = -90)]
    [IdentityProperty]
    [NotAggregated]
    [Dimension(typeof(int), nameof(Month))]
    public int Month { get; init; }

    [Display(Order = -80)]
    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }

    [IdentityProperty]
    [NotVisible]
    public Period Period { get; init; }

    [IdentityProperty]
    [NotVisible]
    [Dimension(typeof(GroupOfContract))]
    public string GroupOfContract { get; init; }
}