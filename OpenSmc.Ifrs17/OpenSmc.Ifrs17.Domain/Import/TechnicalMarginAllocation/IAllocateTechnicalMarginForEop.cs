using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginAllocation;

public interface IAllocateTechnicalMarginForEop : IAllocateTechnicalMargin
{
    double IAllocateTechnicalMargin.Value => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
        .Sum(aoc => GetScope<IAllocateTechnicalMargin>(Identity with { AocType = aoc.AocType, Novelty = aoc.Novelty }).Value);
    [NotVisible] string IAllocateTechnicalMargin.ComputedEstimateType => ComputeEstimateType(AggregatedTechnicalMargin);
}