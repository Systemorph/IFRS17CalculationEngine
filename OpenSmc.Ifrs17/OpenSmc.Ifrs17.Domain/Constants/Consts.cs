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