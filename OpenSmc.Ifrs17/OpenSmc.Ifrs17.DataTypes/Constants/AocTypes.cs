namespace OpenSmc.Ifrs17.Domain.Constants;

public static class AocTypes
{
    public const string BOP = nameof(BOP); // Beginning of Period (opening value of an AOC chain)
    public const string MC = nameof(MC); // Model Corrections (changes to the model)
    public const string PC = nameof(PC); // Portfolio Changes
    public const string RCU = nameof(RCU); // Reinsurance Coverage Update
    public const string CF = nameof(CF); // Cash flow (Nominal)
    public const string IA = nameof(IA); // Interest Accretion
    public const string AU = nameof(AU); // Assumptions Update (changes to general assumptions)
    public const string FAU = nameof(FAU); // Financial Assumptions Update (changes to financial assumptions)
    public const string YCU = nameof(YCU); // Yield Curve Update
    public const string CRU = nameof(CRU); // Credit Default Risk Parameters Update
    public const string WO = nameof(WO); // Write-off
    public const string EV = nameof(EV); // Experience Variance

    public const string CL = nameof(CL); // Combined Liabilities (control run where all changes are calculated together for all novelties)

    public const string EA = nameof(EA); // Experience Adjustment
    public const string AM = nameof(AM); // Amortization
    public const string FX = nameof(FX); // Foreing Exchange
    public const string EOP = nameof(EOP); // End of Period (closing value of an AOC chain)
}