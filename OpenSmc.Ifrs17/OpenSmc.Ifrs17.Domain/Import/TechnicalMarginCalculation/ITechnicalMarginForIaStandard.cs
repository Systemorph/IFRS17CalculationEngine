namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginForIaStandard : ITechnicalMargin, IInterestAccretionFactor
{
    double ITechnicalMargin.Value => AggregatedValue * GetInterestAccretionFactor(EconomicBasis);
}