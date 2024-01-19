namespace OpenSmc.Ifrs17.Domain.Constants;

public static class Consts
{
    public const double Precision = 1E-5;
    public const double ProjectionPrecision = 1E-3;
    public const double BenchmarkPrecision = 1E-4;
    public const double YieldCurvePrecision = 1E-8;
    public const int CurrentPeriod = 0;
    public const int PreviousPeriod = -1;
    public const int MonthInAYear = 12;
    public const int MonthInAQuarter = 3;
    public const int DefaultDataNodeActivationMonth = 1;
    public const double DefaultPremiumExperienceAdjustmentFactor = 1.0;
    public const string Main = nameof(Main);
    public const string Default = nameof(Default);
    public const string ValueType = nameof(ValueType);
    public const string GroupCurrency = "CHF";
}

public static class ImportFormats
{
    public const string Cashflow = nameof(Cashflow); // Importer for Nominal Cash flows
    public const string Actual = nameof(Actual); // Importer for Actuals
    public const string Opening = nameof(Opening); // Importer for Opening Balances (BOP Inforce of CSM/LC)

    public const string SimpleValue = nameof(SimpleValue); // Importer for Simple Values (pre-calculated direct import)

    public const string YieldCurve = nameof(YieldCurve); // Importer for Yield Curve
    public const string DataNode = nameof(DataNode); // Importer for Data Node
    public const string DataNodeState = nameof(DataNodeState); // Importer for Data Node State
    public const string DataNodeParameter = nameof(DataNodeParameter); // Importer for Data Node Parameters

    public const string AocConfiguration = nameof(AocConfiguration); // Importer for Analysis of Change Configuration settings
}

public static class ImportTypes
{
    public const string ExchangeRate = nameof(ExchangeRate);
    public const string PartnerRating = nameof(PartnerRating);
    public const string CreditDefaultRate = nameof(CreditDefaultRate);
}

public static class ValuationApproaches
{
    public const string BBA = nameof(BBA); //Building Block Approach
    public const string VFA = nameof(VFA); //Variable Fee Approach
    public const string PAA = nameof(PAA); //Premium Allocation Approach
}

public static class LiabilityTypes
{
    public const string LRC = nameof(LRC); //Liability for Remaining Coverage
    public const string LIC = nameof(LIC); //Liability Incurred Claims
}

public static class EstimateTypes
{
    public const string BE = nameof(BE); //Best Estimate
    public const string RA = nameof(RA); //Risk Adjustment
    public const string P = nameof(P); //Patterns
    public const string A = nameof(A); //Actuals
    public const string AA = nameof(AA); //Advance Actuals
    public const string OA = nameof(OA); //Overdue Actuals
    public const string DA = nameof(DA); //Deferrable Expenses
    public const string R = nameof(R); //Premium Revenues
    public const string C = nameof(C); //Contractual Service Margin
    public const string L = nameof(L); //Loss Component
    public const string LR = nameof(LR); //Loss Recovery
    public const string F = nameof(F); //Factors
    public const string FCF = nameof(FCF); //Fulfilment Cash flows
    public const string BEPA = nameof(BEPA); //Experience Adjusted BE Premium to Csm
    public const string APA = nameof(APA); //Experience Adjusted Written Actual Premium to Csm

    public const string PCE = nameof(PCE); //Paid Cash Estimate : actuals calculated according to an expected paid pattern provided as a cashflow
}

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

public static class Novelties
{
    public const string I = nameof(I); //In-Force
    public const string N = nameof(N); //New Business
    public const string C = nameof(C); //All Novelties Combined
}

public static class EconomicBases
{
    public const string L = nameof(L); //Locked Interest Rates
    public const string C = nameof(C); //Current Interest Rates
    public const string N = nameof(N); //Nominal
}

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

public static class LineOfBusinesses
{
    public const string LI = nameof(LI); //Life
    public const string NL = nameof(NL); //Non-Life
}

public static class Scenarios
{
    public static bool EnableScenario = true;
    public const string Default = "Best Estimate";
    public const string All = nameof(All);
    public const string Delta = nameof(Delta);
}

public static class ParameterReportType
{
    public const string DataNode = nameof(DataNode);
    public const string DataNodeState = nameof(DataNodeState);
    public const string YieldCurves = nameof(YieldCurves);
    public const string SingleDataNodeParameters = nameof(SingleDataNodeParameters);
    public const string InterDataNodeParameters = nameof(InterDataNodeParameters);
    public const string CurrentPartnerRating = nameof(CurrentPartnerRating);
    public const string CurrentPartnerDefaultRates = nameof(CurrentPartnerDefaultRates);
    public const string LockedInPartnerRating = nameof(LockedInPartnerRating);
    public const string LockedInPartnerDefaultRates = nameof(LockedInPartnerDefaultRates);
}

public static class Debug
{
    public static bool Enable = false;
}

public static class Projection
{
    public static bool Enable = true;
    public static bool EnableWithCutoff = false;
    public static int Cutoff = default;
}