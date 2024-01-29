namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginAllocation;

public interface IComputeIAllocateTechnicalMarginWithIfrsVariable : IAllocateTechnicalMargin
{
    double IAllocateTechnicalMargin.TechnicalMargin => GetScope<IAllocateTechnicalMarginWithIfrsVariable>(Identity).Value;
    double IAllocateTechnicalMargin.AggregatedTechnicalMargin => GetScope<IAllocateTechnicalMarginWithIfrsVariable>(Identity).AggregatedValue;
}