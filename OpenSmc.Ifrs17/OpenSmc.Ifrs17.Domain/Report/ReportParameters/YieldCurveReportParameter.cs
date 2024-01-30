using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportParameters;

public record YieldCurveReportParameter : ReportParameter
{
    [IdentityProperty]
    [NotVisible]
    public string? YieldCurveType { get; init; }

    [Dimension(typeof(Currency))]
    public string? Currency { get; init; }

    public string? Name { get; init; }
}