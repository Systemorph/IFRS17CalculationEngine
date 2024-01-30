using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Report.ReportParameters;

public record DataNodeStateReportParameter : ReportParameter
{
    public State State { get; init; }
}