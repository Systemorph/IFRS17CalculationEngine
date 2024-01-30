using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportParameters;

public record SingleDataNodeReportParameter : ReportParameter
{

    public double PremiumAllocation { get; init; }

    [Dimension(typeof(CashFlowPeriodicity))]
    public CashFlowPeriodicity CashFlowPeriodicity { get; init; }

    [Dimension(typeof(InterpolationMethod))]
    public InterpolationMethod InterpolationMethod { get; init; }
}