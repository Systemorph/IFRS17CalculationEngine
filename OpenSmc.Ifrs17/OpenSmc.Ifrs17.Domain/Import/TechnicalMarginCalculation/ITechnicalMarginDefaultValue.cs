namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginDefaultValue : ITechnicalMargin
{
    double ITechnicalMargin.Value => default;
}