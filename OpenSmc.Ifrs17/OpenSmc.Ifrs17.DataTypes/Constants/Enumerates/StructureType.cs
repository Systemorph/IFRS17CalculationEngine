namespace OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;

[Flags]
public enum StructureType
{
    None = 1,
    AocPresentValue = 2,
    AocAccrual = 4,
    AocTechnicalMargin = 8
}