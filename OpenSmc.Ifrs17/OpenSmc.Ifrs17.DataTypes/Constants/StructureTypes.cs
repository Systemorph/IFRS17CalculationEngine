namespace OpenSmc.Ifrs17.DataTypes.Constants;

public static class StructureTypes
{
    public const string None = "NO";
    public const string AocPresentValue = "PV";
    public const string AocAccrual = "AC";
    public const string AocTechnicalMargin = "TM";
    public const string AocPvAc = "PV|AC";
    public const string AocPvTm = "PV|TM";
    public const string AocAcTm = "AC|TM";
    public const string AocPvAcTm = "PV|AC|TM";
}

public static class EstimateTypeStructureTypes
{
    public const string None = default(string);
    public const string AoC = "AoC";
}