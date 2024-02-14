using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.Args;

public record ReportArgs : Args
{
    public string HierarchyName { get; init; }

    public CurrencyType CurrencyType { get; init; }

    public string ReportName { get; init; } // this is the key to which data to load (like loading behavior). If null, loads everything

    public ReportArgs(string reportingNode, int year, int month, Periodicity periodicity,
        string scenario, string hierarchyName, CurrencyType currencyType)
        : base(reportingNode, year, month, periodicity, scenario)
    {
        CurrencyType = currencyType;
        HierarchyName = hierarchyName;
    }
}