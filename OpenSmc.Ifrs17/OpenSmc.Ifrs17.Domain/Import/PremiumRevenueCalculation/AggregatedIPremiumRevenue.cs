using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;

public interface AggregatedIPremiumRevenue : IPremiumRevenue
{
    double AggregatedValue => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
        .Sum(aoc => GetScope<IPremiumRevenue>(Identity with { AocType = aoc.AocType, Novelty = aoc.Novelty }).Value);
}