namespace OpenSmc.Ifrs17.Domain.Constants.Enumerates;

[Flags]
public enum PortfolioView
{
    Gross = 1,
    Reinsurance = 2,
    Net = Gross | Reinsurance
}