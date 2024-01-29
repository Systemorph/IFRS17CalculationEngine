using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;

public interface IDeferrableEop : IDiscountedDeferrable
{
    double IDiscountedDeferrable.Value => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
        .Sum(aocStep => GetScope<IDiscountedDeferrable>(Identity with { AocType = aocStep.AocType, Novelty = aocStep.Novelty }).Value);
}