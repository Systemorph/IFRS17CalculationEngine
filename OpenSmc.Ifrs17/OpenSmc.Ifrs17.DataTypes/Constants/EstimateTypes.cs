namespace OpenSmc.Ifrs17.Domain.Constants;

public static class EstimateTypes
{
    public const string BE = nameof(BE); //Best Estimate
    public const string RA = nameof(RA); //Risk Adjustment
    public const string P = nameof(P); //Patterns
    public const string A = nameof(A); //Actuals
    public const string AA = nameof(AA); //Advance Actuals
    public const string OA = nameof(OA); //Overdue Actuals
    public const string DA = nameof(DA); //Deferrable Expenses
    public const string R = nameof(R); //Premium IRevenues
    public const string C = nameof(C); //Contractual Service Margin
    public const string L = nameof(L); //Loss Component
    public const string LR = nameof(LR); //Loss Recovery
    public const string F = nameof(F); //Factors
    public const string FCF = nameof(FCF); //Fulfilment Cash flows
    public const string BEPA = nameof(BEPA); //Experience Adjusted BE Premium to ICsm
    public const string APA = nameof(APA); //Experience Adjusted Written IActual Premium to ICsm

    public const string PCE = nameof(PCE); //Paid Cash Estimate : actuals calculated according to an expected paid pattern provided as a cashflow
}