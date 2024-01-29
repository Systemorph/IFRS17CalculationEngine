using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;

public interface IPremiumRevenueAfterFirstYear : IPremiumRevenue
{
    double IPremiumRevenue.Value => GetScope<IPremiumRevenue>(Identity with { AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.ProjectionPeriod - 1 }).Value;
}