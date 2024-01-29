using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;

public interface IDeferrableForIaStandard : IDiscountedDeferrable, IInterestAccretionFactor
{
    private double AggregatedValue => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
        .Sum(aoc => GetScope<IDiscountedDeferrable>(Identity with { AocType = aoc.AocType, Novelty = aoc.Novelty }).Value);
    double IDiscountedDeferrable.Value => AggregatedValue * GetInterestAccretionFactor(EconomicBasis);
}