using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportParameters;

public record InterDataNodeReportParameter : ReportParameter
{
    [Dimension(typeof(GroupOfContract), nameof(LinkedDataNode))]
    public string? LinkedDataNode { get; init; }

    public double ReinsuranceCoverage { get; init; }
}