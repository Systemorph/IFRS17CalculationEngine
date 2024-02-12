namespace OpenSmc.Ifrs17.Domain.Constants.Enumerates;

[Flags]
public enum DataType
{
    Optional = 1,
    Mandatory = 2,
    Calculated = 4,
    CalculatedTelescopic = 8,
    CalculatedProjection = 16
}