using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginAllocation;

public interface IAllocateTechnicalMarginForCl : IAllocateTechnicalMargin
{
    private double BalancingValue => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
        .GroupBy(x => x.Novelty, (k, v) => v.Last())
        .Sum(aoc =>
        {
            var id = Identity with { AocType = aoc.AocType, Novelty = aoc.Novelty };
            return GetScope<IAllocateTechnicalMargin>(id).ComputedEstimateType != ComputedEstimateType ?
                GetScope<IAllocateTechnicalMargin>(id).TechnicalMargin + GetScope<IAllocateTechnicalMargin>(id).AggregatedTechnicalMargin
                : (double)default;
        });

    [NotVisible] bool IAllocateTechnicalMargin.HasSwitch => Math.Abs(BalancingValue) > Consts.Precision;
    [NotVisible] double IAllocateTechnicalMargin.AggregatedTechnicalMargin => BalancingValue;
}