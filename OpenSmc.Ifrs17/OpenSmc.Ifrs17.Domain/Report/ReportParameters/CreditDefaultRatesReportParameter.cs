using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Attributes.Arithmetics;

namespace OpenSmc.Ifrs17.Domain.Report.ReportParameters;

public record CreditDefaultRatesReportParameter : ReportParameter
{
    [IdentityProperty]
    [NotAggregated]
    [Dimension(typeof(int), nameof(InitialYear))]
    [NotVisible]
    public int InitialYear { get; init; }

    [IdentityProperty]
    [NotVisible]
    public string? CreditDefaultRatesType { get; init; }

    [IdentityProperty]
    [Dimension(typeof(CreditRiskRating))]
    [NotVisible]
    public string? CreditRiskRating { get; init; }
}