namespace OpenSmc.Ifrs17.DataTypes.Constants;

public static class StructureType
{
    public const string NO = nameof(NO); //None
    public const string PV = nameof(PV); //AocPresentValue
    public const string AC = nameof(AC); //AocAccrual
    public const string TM = nameof(TM); //AocTecnicalMargin
    public const string PVAC = "PV|AC"; //AocPvAc
    public const string PVTM = "PV|TM"; //AocPVTm
    public const string ACTM = "AC|TM"; //AocAcTm
    public const string PVACTM = "PV|AC|TM"; //AocPvAcTm
}