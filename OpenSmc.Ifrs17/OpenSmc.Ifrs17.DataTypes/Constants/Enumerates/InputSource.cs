namespace OpenSmc.Ifrs17.Domain.Constants.Enumerates;

[Flags]
public enum InputSource
{
    NotApplicable = 0,
    Opening = 1,
    Actual = 2,
    Cashflow = 4
}