using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.DataModel.Args;

public record ImportArgs : Args
{
    public string ImportFormat { get; init; }

    public ImportArgs(string reportingNode, int year, int month, Periodicity periodicity, string scenario, string importFormat)
        : base(reportingNode, year, month, periodicity, scenario)
    {
        ImportFormat = importFormat;
    }
}