using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginForEop : ITechnicalMargin
{
    double ITechnicalMargin.Value => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
        .Sum(aoc => GetScope<ITechnicalMargin>(Identity with { AocType = aoc.AocType, Novelty = aoc.Novelty }).Value);
}