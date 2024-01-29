namespace OpenSmc.Ifrs17.Domain.Constants;

public static class AmountTypes
{
    public const string ACA = nameof(ACA); // Attributable Commissions Acquisition
    public const string AEA = nameof(AEA); // Attributable Expenses Acquisition
    public const string CDR = nameof(CDR); // Credit Default Risk

    public const string CDRI = nameof(CDRI); // Initial Credit Default Risk, i.e. the CDR value when the GIC state is set to active

    public const string CL = nameof(CL); // Claims
    public const string PR = nameof(PR); // Premiums
    public const string NIC = nameof(NIC); // Claims Non-Investment component
    public const string ICO = nameof(ICO); // Claims Investment component
    public const string NE = nameof(NE); // Non Attributable Expenses
    public const string ACM = nameof(ACM); // Attributable Commissions Maintenance
    public const string AEM = nameof(AEM); // Attributable Expenses Maintenance
    public const string AC = nameof(AC); // Attributable Commissions
    public const string AE = nameof(AE); // Attributable Expenses
    public const string CE = nameof(CE); // Claim Expenses
    public const string ALE = nameof(ALE); // Allocated Loss Adjustment Expenses
    public const string ULE = nameof(ULE); // Unallocated Loss Adjustment Expenses
    public const string CU = nameof(CU); // Coverage Units
    public const string DE = nameof(DE); // Deferrable Expenses
    public const string DAE = nameof(DAE); // Deferrable Acquisition Expenses
}