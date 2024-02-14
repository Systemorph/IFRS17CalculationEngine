namespace OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;

[Flags]
public enum PortfolioView
{
    Gross = 1,
    Reinsurance = 2,
    Net = Gross | Reinsurance
}