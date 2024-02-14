using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.Args;

public record ImportArgs : Args
{
    public string ImportFormat { get; init; }

    public ImportArgs(string reportingNode, int year, int month, Periodicity periodicity, string scenario, string importFormat)
        : base(reportingNode, year, month, periodicity, scenario)
    {
        ImportFormat = importFormat;
    }
}