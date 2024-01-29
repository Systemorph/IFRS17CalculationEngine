namespace OpenSmc.Ifrs17.Domain.Constants;

public static class ImportFormats
{
    public const string Cashflow = nameof(Cashflow); // Importer for Nominal Cash flows
    public const string Actual = nameof(Actual); // Importer for Actuals
    public const string Opening = nameof(Opening); // Importer for Opening Balances (BOP Inforce of CSM/LC)

    public const string SimpleValue = nameof(SimpleValue); // Importer for Simple Values (pre-calculated direct import)

    public const string YieldCurve = nameof(YieldCurve); // Importer for Yield Curve
    public const string DataNode = nameof(DataNode); // Importer for Data Node
    public const string DataNodeState = nameof(DataNodeState); // Importer for Data Node State
    public const string DataNodeParameter = nameof(DataNodeParameter); // Importer for Data Node Parameters

    public const string AocConfiguration = nameof(AocConfiguration); // Importer for Analysis of Change Configuration settings
}