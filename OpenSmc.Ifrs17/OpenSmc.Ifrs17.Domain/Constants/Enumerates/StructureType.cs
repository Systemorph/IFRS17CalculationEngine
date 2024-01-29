namespace OpenSmc.Ifrs17.Domain.Constants.Enumerates;

[Flags]
public enum StructureType
{
    None = 1,
    AocPresentValue = 2,
    AocAccrual = 4,
    AocTechnicalMargin = 8
}