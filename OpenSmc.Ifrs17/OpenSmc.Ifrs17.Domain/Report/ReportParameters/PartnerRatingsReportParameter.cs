using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Attributes.Arithmetics;

namespace OpenSmc.Ifrs17.Domain.Report.ReportParameters;

public record PartnerRatingsReportParameter : ReportParameter
{
    [IdentityProperty]
    [NotAggregated]
    [Dimension(typeof(int), nameof(InitialYear))]
    [NotVisible]
    public int InitialYear { get; init; }

    [IdentityProperty]
    [NotVisible]
    public string? PartnerRatingType { get; init; }

    [IdentityProperty]
    [NotVisible]
    [Dimension(typeof(Partner))]
    public string? Partner { get; init; }

    [Dimension(typeof(CreditRiskRating))]
    public string? CreditRiskRating { get; init; }
}