namespace OpenSmc.Ifrs17.Domain.Import.LossRecoveryComponent;

public interface ILossRecoveryComponentPaa : ILossRecoveryComponent
{
    double ILossRecoveryComponent.Value => -1d * LoReCoBoundaryValue;
}