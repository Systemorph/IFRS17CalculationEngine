namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginAllocation;

public interface IAllocateTechnicalMarginForBop : IAllocateTechnicalMargin
{
    bool IAllocateTechnicalMargin.HasSwitch => false;
}