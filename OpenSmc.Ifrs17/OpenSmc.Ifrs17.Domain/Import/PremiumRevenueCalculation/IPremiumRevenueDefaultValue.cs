namespace OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;

public interface IPremiumRevenueDefaultValue : IPremiumRevenue
{
    double IPremiumRevenue.Value => default;
}