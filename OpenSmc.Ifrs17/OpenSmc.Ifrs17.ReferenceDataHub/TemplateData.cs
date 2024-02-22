using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

public class TemplateData
{
    public static readonly Dictionary<Type, IEnumerable<AocConfiguration>> AocConfiguration   
        =
        new()
        {
            {
                typeof(AocConfiguration), new[]
                {
                    new AocConfiguration
                    {
                        AocType = "BOP", Novelty = "I", DataType = DataType.Optional | DataType.CalculatedProjection,
                        InputSource = InputSource.Opening | InputSource.Actual | InputSource.Cashflow,
                        StructureType = "PV|AC|TM", FxPeriod = FxPeriod.BeginningOfPeriod,
                        YcPeriod = PeriodType.BeginningOfPeriod, CdrPeriod = PeriodType.BeginningOfPeriod,
                        ValuationPeriod = ValuationPeriod.BeginningOfPeriod, RcPeriod = PeriodType.BeginningOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "MC", Novelty = "I", DataType = DataType.Optional, InputSource = InputSource.Cashflow,
                        StructureType = "PV|TM", FxPeriod = FxPeriod.BeginningOfPeriod,
                        YcPeriod = PeriodType.BeginningOfPeriod, CdrPeriod = PeriodType.BeginningOfPeriod,
                        ValuationPeriod = ValuationPeriod.BeginningOfPeriod, RcPeriod = PeriodType.BeginningOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "RCU", Novelty = "I", DataType = DataType.Calculated,
                        InputSource = InputSource.Cashflow, StructureType = "TM", FxPeriod = FxPeriod.BeginningOfPeriod,
                        YcPeriod = PeriodType.BeginningOfPeriod, CdrPeriod = PeriodType.BeginningOfPeriod,
                        ValuationPeriod = ValuationPeriod.BeginningOfPeriod, RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "CF", Novelty = "I", DataType = DataType.Calculated | DataType.CalculatedProjection,
                        InputSource = InputSource.Cashflow, StructureType = "PV", FxPeriod = FxPeriod.Average,
                        YcPeriod = PeriodType.NotApplicable, CdrPeriod = PeriodType.BeginningOfPeriod,
                        ValuationPeriod = ValuationPeriod.Delta, RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "IA", Novelty = "I", DataType = DataType.Calculated | DataType.CalculatedProjection,
                        InputSource = InputSource.Opening | InputSource.Cashflow, StructureType = "PV|TM",
                        FxPeriod = FxPeriod.Average, YcPeriod = PeriodType.BeginningOfPeriod,
                        CdrPeriod = PeriodType.BeginningOfPeriod, ValuationPeriod = ValuationPeriod.Delta,
                        RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "AU", Novelty = "I", DataType = DataType.Optional, InputSource = InputSource.Cashflow,
                        StructureType = "PV|TM", FxPeriod = FxPeriod.EndOfPeriod,
                        YcPeriod = PeriodType.BeginningOfPeriod, CdrPeriod = PeriodType.BeginningOfPeriod,
                        ValuationPeriod = ValuationPeriod.EndOfPeriod, RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "YCU", Novelty = "I", DataType = DataType.CalculatedTelescopic,
                        InputSource = InputSource.Cashflow, StructureType = "PV|TM", FxPeriod = FxPeriod.EndOfPeriod,
                        YcPeriod = PeriodType.EndOfPeriod, CdrPeriod = PeriodType.BeginningOfPeriod,
                        ValuationPeriod = ValuationPeriod.EndOfPeriod, RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "CRU", Novelty = "I", DataType = DataType.CalculatedTelescopic,
                        InputSource = InputSource.Cashflow, StructureType = "PV|TM", FxPeriod = FxPeriod.EndOfPeriod,
                        YcPeriod = PeriodType.EndOfPeriod, CdrPeriod = PeriodType.EndOfPeriod,
                        ValuationPeriod = ValuationPeriod.EndOfPeriod, RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "EV", Novelty = "I", DataType = DataType.Optional, InputSource = InputSource.Cashflow,
                        StructureType = "PV|TM", FxPeriod = FxPeriod.EndOfPeriod, YcPeriod = PeriodType.EndOfPeriod,
                        CdrPeriod = PeriodType.EndOfPeriod, ValuationPeriod = ValuationPeriod.EndOfPeriod,
                        RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "BOP", Novelty = "N", DataType = DataType.Optional,
                        InputSource = InputSource.Cashflow, StructureType = "PV|TM", FxPeriod = FxPeriod.Average,
                        YcPeriod = PeriodType.EndOfPeriod, CdrPeriod = PeriodType.EndOfPeriod,
                        ValuationPeriod = ValuationPeriod.BeginningOfPeriod, RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "MC", Novelty = "N", DataType = DataType.Optional, InputSource = InputSource.Cashflow,
                        StructureType = "PV|TM", FxPeriod = FxPeriod.Average, YcPeriod = PeriodType.EndOfPeriod,
                        CdrPeriod = PeriodType.EndOfPeriod, ValuationPeriod = ValuationPeriod.BeginningOfPeriod,
                        RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "CF", Novelty = "N", DataType = DataType.Calculated,
                        InputSource = InputSource.Cashflow, StructureType = "PV", FxPeriod = FxPeriod.Average,
                        YcPeriod = PeriodType.NotApplicable, CdrPeriod = PeriodType.EndOfPeriod,
                        ValuationPeriod = ValuationPeriod.Delta, RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "IA", Novelty = "N", DataType = DataType.Calculated,
                        InputSource = InputSource.Cashflow, StructureType = "PV|TM", FxPeriod = FxPeriod.Average,
                        YcPeriod = PeriodType.EndOfPeriod, CdrPeriod = PeriodType.EndOfPeriod,
                        ValuationPeriod = ValuationPeriod.Delta, RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "AU", Novelty = "N", DataType = DataType.Optional, InputSource = InputSource.Cashflow,
                        StructureType = "PV|TM", FxPeriod = FxPeriod.EndOfPeriod, YcPeriod = PeriodType.EndOfPeriod,
                        CdrPeriod = PeriodType.EndOfPeriod, ValuationPeriod = ValuationPeriod.EndOfPeriod,
                        RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "EV", Novelty = "N", DataType = DataType.Optional, InputSource = InputSource.Cashflow,
                        StructureType = "PV|TM", FxPeriod = FxPeriod.EndOfPeriod, YcPeriod = PeriodType.EndOfPeriod,
                        CdrPeriod = PeriodType.EndOfPeriod, ValuationPeriod = ValuationPeriod.EndOfPeriod,
                        RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "CL", Novelty = "C", DataType = DataType.Mandatory,
                        InputSource = InputSource.Cashflow, StructureType = "PV|TM", FxPeriod = FxPeriod.EndOfPeriod,
                        YcPeriod = PeriodType.EndOfPeriod, CdrPeriod = PeriodType.EndOfPeriod,
                        ValuationPeriod = ValuationPeriod.EndOfPeriod, RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "EA", Novelty = "C", DataType = DataType.Calculated,
                        InputSource = InputSource.Cashflow, StructureType = "TM", FxPeriod = FxPeriod.EndOfPeriod,
                        YcPeriod = PeriodType.NotApplicable, CdrPeriod = PeriodType.NotApplicable,
                        ValuationPeriod = ValuationPeriod.NotApplicable, RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "CF", Novelty = "C", DataType = DataType.Optional | DataType.CalculatedProjection,
                        InputSource = InputSource.Actual | InputSource.Cashflow, StructureType = "AC",
                        FxPeriod = FxPeriod.Average, YcPeriod = PeriodType.NotApplicable,
                        CdrPeriod = PeriodType.NotApplicable, ValuationPeriod = ValuationPeriod.NotApplicable,
                        RcPeriod = PeriodType.NotApplicable,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "WO", Novelty = "C", DataType = DataType.Optional | DataType.CalculatedProjection,
                        InputSource = InputSource.Actual, StructureType = "AC", FxPeriod = FxPeriod.Average,
                        YcPeriod = PeriodType.NotApplicable, CdrPeriod = PeriodType.NotApplicable,
                        ValuationPeriod = ValuationPeriod.NotApplicable, RcPeriod = PeriodType.NotApplicable,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "AM", Novelty = "C", DataType = DataType.Calculated,
                        InputSource = InputSource.Actual | InputSource.Cashflow, StructureType = "TM",
                        FxPeriod = FxPeriod.EndOfPeriod, YcPeriod = PeriodType.NotApplicable,
                        CdrPeriod = PeriodType.NotApplicable, ValuationPeriod = ValuationPeriod.NotApplicable,
                        RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                    new AocConfiguration
                    {
                        AocType = "EOP", Novelty = "C", DataType = DataType.Calculated,
                        InputSource = InputSource.Actual | InputSource.Cashflow, StructureType = "PV|AC|TM",
                        FxPeriod = FxPeriod.EndOfPeriod, YcPeriod = PeriodType.EndOfPeriod,
                        CdrPeriod = PeriodType.EndOfPeriod, ValuationPeriod = ValuationPeriod.EndOfPeriod,
                        RcPeriod = PeriodType.EndOfPeriod,
                        Year = 1999, Month = 1,
                    },
                }
            },
        };

    public static readonly Dictionary<Type, IEnumerable<object>> TemplateReferenceData
        =
        new()
        {
            {
                typeof(AmountType),
                new[]
                {
                    new AmountType
                    {
                        SystemName = "PR", DisplayName = "Premiums", Parent = null, Order = 10,
                        PeriodType = PeriodType.BeginningOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "CL", DisplayName = "Claims", Parent = null, Order = 20,
                        PeriodType = PeriodType.EndOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "NIC", DisplayName = "Non Investment Component", Parent = "CL", Order = 30,
                        PeriodType = PeriodType.EndOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "ICO", DisplayName = "Investment Component", Parent = "CL", Order = 40,
                        PeriodType = PeriodType.EndOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "CDR", DisplayName = "Credit Default Risk", Parent = "CL", Order = 50,
                        PeriodType = PeriodType.EndOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "CDRI", DisplayName = "Initial Credit Default Risk", Parent = "CDR", Order = 60,
                        PeriodType = PeriodType.EndOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "CE", DisplayName = "Claim Expenses", Parent = "CL", Order = 200,
                        PeriodType = PeriodType.EndOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "ALE", DisplayName = "Allocated Loss Adjustment Expenses", Parent = "CE",
                        Order = 210, PeriodType = PeriodType.EndOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "ULE", DisplayName = "Unallocated Loss Adjustment Expenses", Parent = "CE",
                        Order = 220, PeriodType = PeriodType.EndOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "AE", DisplayName = "Attributable Expenses", Parent = null, Order = 80,
                        PeriodType = PeriodType.EndOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "AEA", DisplayName = "Aquisition", Parent = "AE", Order = 90,
                        PeriodType = PeriodType.BeginningOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "AEM", DisplayName = "Maintenance", Parent = "AE", Order = 100,
                        PeriodType = PeriodType.EndOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "NE", DisplayName = "Non Attributable Expenses", Parent = null, Order = 110,
                        PeriodType = PeriodType.BeginningOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "AC", DisplayName = "Attributable Commission", Parent = null, Order = 120,
                        PeriodType = PeriodType.BeginningOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "ACA", DisplayName = "Aquisition", Parent = "AC", Order = 130,
                        PeriodType = PeriodType.BeginningOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "ACM", DisplayName = "Maitenance", Parent = "AC", Order = 140,
                        PeriodType = PeriodType.EndOfPeriod
                    },
                    new AmountType
                    {
                        SystemName = "CU", DisplayName = "Coverage Units", Parent = null, Order = 150,
                        PeriodType = PeriodType.EndOfPeriod
                    },
                }
            },

            {
                typeof(DeferrableAmountType), new[]
                {
                    new DeferrableAmountType
                    {
                        SystemName = "DE", DisplayName = "Deferrals", Parent = null, Order = 10,
                        PeriodType = PeriodType.BeginningOfPeriod
                    },
                    new DeferrableAmountType
                    {
                        SystemName = "DAE", DisplayName = "Acquisition Expenses", Parent = "DE", Order = 20,
                        PeriodType = PeriodType.BeginningOfPeriod
                    },
                }
            },
            {
                typeof(AocType), new []
                {
                    new AocType { SystemName = "BOP", DisplayName = "Opening Balance", Order = 10, },
                    new AocType { SystemName = "MC", DisplayName = "Model Correction", Order = 20, },
                    new AocType { SystemName = "PC", DisplayName = "Portfolio Changes", Order = 30, },
                    new AocType
                    {
                        SystemName = "RCU", DisplayName = "Reinsurance Coverage Update", Order = 40,
                    },
                    new AocType { SystemName = "CF", DisplayName = "Cash flow", Order = 50, },
                    new AocType { SystemName = "IA", DisplayName = "Interest Accretion", Order = 60, },
                    new AocType { SystemName = "AU", DisplayName = "Assumption Update", Order = 70, },
                    new AocType
                    {
                        SystemName = "FAU", DisplayName = "Financial Assumption Update", Order = 80,
                    },
                    new AocType { SystemName = "YCU", DisplayName = "Yield Curve Update", Order = 90, },
                    new AocType { SystemName = "CRU", DisplayName = "Credit Risk Update", Order = 100, },
                    new AocType { SystemName = "EV", DisplayName = "Experience Variance", Order = 110, },
                    new AocType { SystemName = "WO", DisplayName = "Write-Off", Order = 120, },
                    new AocType
                    {
                        SystemName = "CL", DisplayName = "Combined Liabilities", Order = 130,
                    },
                    new AocType
                    {
                        SystemName = "EA", DisplayName = "Experience Adjustment", Order = 140,
                    },
                    new AocType { SystemName = "AM", DisplayName = "Amortization", Order = 150, },
                    new AocType { SystemName = "FX", DisplayName = "FX Impact", Order = 160, },
                    new AocType { SystemName = "EOP", DisplayName = "Closing Balance", Order = 170, },

                }
            },
           
            {
                typeof(StructureType), new []
                {
                    new StructureType { SystemName = "NO", DisplayName = "None" },
                    new StructureType { SystemName = "PV", DisplayName = "AocPresentValue" },
                    new StructureType { SystemName = "AC", DisplayName = "AocAccrual" },
                    new StructureType { SystemName = "TM", DisplayName = "AocTechnicalMargin" },
                    new StructureType { SystemName = "PV|AC", DisplayName = "AocPvAc" },
                    new StructureType { SystemName = "PV|TM", DisplayName = "AocPvTmg" },
                    new StructureType { SystemName = "AC|TM", DisplayName = "AocAcTm" },
                    new StructureType { SystemName = "PV|AC|TM", DisplayName = "AocPvAcTm" },
                }
            },
            {
                typeof(CreditRiskRating), new []
                {
                    new CreditRiskRating { SystemName = "AAA", DisplayName = "AAA" },
                    new CreditRiskRating { SystemName = "AA+", DisplayName = "AA+" },
                    new CreditRiskRating { SystemName = "AA", DisplayName = "AA" },
                    new CreditRiskRating { SystemName = "AA-", DisplayName = "AA-" },
                    new CreditRiskRating { SystemName = "A+", DisplayName = "A+" },
                    new CreditRiskRating { SystemName = "A", DisplayName = "A" },
                    new CreditRiskRating { SystemName = "A-", DisplayName = "A-" },
                    new CreditRiskRating { SystemName = "BBB+", DisplayName = "BBB+" },
                    new CreditRiskRating { SystemName = "BBB", DisplayName = "BBB" },
                    new CreditRiskRating { SystemName = "BBB-", DisplayName = "BBB-" },
                    new CreditRiskRating { SystemName = "BB+", DisplayName = "BB+" },
                    new CreditRiskRating { SystemName = "BB", DisplayName = "BB" },
                    new CreditRiskRating { SystemName = "BB-", DisplayName = "BB-" },
                    new CreditRiskRating { SystemName = "B+", DisplayName = "B+" },
                    new CreditRiskRating { SystemName = "B", DisplayName = "B" },
                    new CreditRiskRating { SystemName = "B-", DisplayName = "B-" },
                    new CreditRiskRating { SystemName = "CCC+", DisplayName = "CCC+" },
                    new CreditRiskRating { SystemName = "CCC", DisplayName = "CCC" },
                    new CreditRiskRating { SystemName = "CCC-", DisplayName = "CCC-" },
                    new CreditRiskRating { SystemName = "CC", DisplayName = "CC" },
                    new CreditRiskRating { SystemName = "C", DisplayName = "C" },
                    new CreditRiskRating { SystemName = "I", DisplayName = "I" },
                }
            },
            {
                typeof(Currency), new []
                {
                    new Currency { SystemName = "USD", DisplayName = "United States Dollar" },
                    new Currency { SystemName = "CHF", DisplayName = "Swiss Franc" },
                    new Currency { SystemName = "DKK", DisplayName = "Danish Krone" },
                    new Currency { SystemName = "EUR", DisplayName = "Euro" },
                    new Currency { SystemName = "GBP", DisplayName = "British Pound" },
                    new Currency { SystemName = "HKD", DisplayName = "Hong Kong Dollar" },
                    new Currency { SystemName = "ITL", DisplayName = "Italian Lira" },
                    new Currency { SystemName = "PLN", DisplayName = "Polish Zloty" },
                    new Currency { SystemName = "SKK", DisplayName = "Slovakian Krona" },
                    new Currency { SystemName = "XTSHY", DisplayName = "Testing Currency (High Yields)" },
                }
            },
            {
                typeof(EconomicBasis), new []
                {
                    new EconomicBasis { SystemName = "N", DisplayName = "Nominal" },
                    new EconomicBasis { SystemName = "L", DisplayName = "Locked-in" },
                    new EconomicBasis { SystemName = "C", DisplayName = "Current" },
                }
            },
            {
                typeof(EstimateType), new []
                {
                    new EstimateType
                    {
                        SystemName = "BE", DisplayName = "Best Estimate of Present Value", Order = 1,
                        StructureType = "PV", InputSource = InputSource.Cashflow, PeriodType = PeriodType.EndOfPeriod
                    },
                    new EstimateType
                    {
                        SystemName = "RA", DisplayName = "Risk Adjustment", Order = 10, StructureType = "PV",
                        InputSource = InputSource.Cashflow, PeriodType = PeriodType.EndOfPeriod
                    },
                    new EstimateType
                    {
                        SystemName = "P", DisplayName = "Patterns", Order = 15, StructureType = "PV",
                        InputSource = InputSource.Cashflow, PeriodType = PeriodType.EndOfPeriod
                    },
                    new EstimateType
                    {
                        SystemName = "C", DisplayName = "Contractual Service Margin", Order = 20, StructureType = "TM",
                        InputSource = InputSource.Opening | InputSource.Actual | InputSource.Cashflow,
                        PeriodType = PeriodType.NotApplicable
                    },
                    new EstimateType
                    {
                        SystemName = "L", DisplayName = "Loss Component", Order = 30, StructureType = "TM",
                        InputSource = InputSource.Opening | InputSource.Actual | InputSource.Cashflow,
                        PeriodType = PeriodType.NotApplicable
                    },
                    new EstimateType
                    {
                        SystemName = "LR", DisplayName = "Loss Recovery Component", Order = 40, StructureType = "TM",
                        InputSource = InputSource.Opening | InputSource.Actual | InputSource.Cashflow,
                        PeriodType = PeriodType.NotApplicable
                    },
                    new EstimateType
                    {
                        SystemName = "PL", DisplayName = "Profit and Loss", Order = 50, StructureType = "NO",
                        InputSource = InputSource.Opening | InputSource.Actual | InputSource.Cashflow,
                        PeriodType = PeriodType.NotApplicable
                    },
                    new EstimateType
                    {
                        SystemName = "AA", DisplayName = "Advance Actuals", Order = 60, StructureType = "AC",
                        InputSource = InputSource.Opening | InputSource.Actual | InputSource.Cashflow,
                        PeriodType = PeriodType.NotApplicable
                    },
                    new EstimateType
                    {
                        SystemName = "OA", DisplayName = "Overdue Actuals", Order = 70, StructureType = "AC",
                        InputSource = InputSource.Opening | InputSource.Actual | InputSource.Cashflow,
                        PeriodType = PeriodType.NotApplicable
                    },
                    new EstimateType
                    {
                        SystemName = "DA", DisplayName = "Deferrable", Order = 80, StructureType = "TM",
                        InputSource = InputSource.Opening | InputSource.Actual | InputSource.Cashflow,
                        PeriodType = PeriodType.NotApplicable
                    },
                    new EstimateType
                    {
                        SystemName = "R", DisplayName = "PAA Revenues", Order = 85, StructureType = "TM",
                        InputSource = InputSource.Cashflow, PeriodType = PeriodType.BeginningOfPeriod
                    },
                    new EstimateType
                    {
                        SystemName = "A", DisplayName = "Actuals", Order = 90, StructureType = "NO",
                        InputSource = InputSource.Actual | InputSource.Cashflow, PeriodType = PeriodType.NotApplicable
                    },
                    new EstimateType
                    {
                        SystemName = "F", DisplayName = "Factors", Order = 100, StructureType = "NO",
                        InputSource = InputSource.Cashflow, PeriodType = PeriodType.NotApplicable
                    },
                    new EstimateType
                    {
                        SystemName = "BEPA", DisplayName = "Best Estimate of Present Value To Csm", Order = 110,
                        StructureType = "NO", InputSource = InputSource.Cashflow, PeriodType = PeriodType.NotApplicable
                    },
                    new EstimateType
                    {
                        SystemName = "APA", DisplayName = "Actuals To Csm", Order = 120, StructureType = "NO",
                        InputSource = InputSource.Actual | InputSource.Cashflow, PeriodType = PeriodType.NotApplicable
                    },

                }
            },
            {
                typeof(LiabilityType), new []
                {
                    new LiabilityType { SystemName = "LRC", DisplayName = "Liability for Remaining Coverage" },
                    new LiabilityType { SystemName = "LIC", DisplayName = "Liabilities for Incurred Claims" },
                }
            },
            {
                typeof(LineOfBusiness), new []
                {
                    new LineOfBusiness { SystemName = "M", DisplayName = "Multiline Life and Non-Life" },
                    new LineOfBusiness { SystemName = "LI", DisplayName = "Life", Parent = "M" },
                    new LineOfBusiness { SystemName = "NL", DisplayName = "Non-Life", Parent = "M" },
                    new LineOfBusiness { SystemName = "LIA", DisplayName = "Liability", Parent = "NL" },
                    new LineOfBusiness { SystemName = "MAE", DisplayName = "Marine, Aviation & Energy", Parent = "NL" },
                    new LineOfBusiness { SystemName = "MOT", DisplayName = "Motor", Parent = "NL" },
                    new LineOfBusiness { SystemName = "NAH", DisplayName = "Non-Life Accident & Health", Parent = "NL" },
                    new LineOfBusiness { SystemName = "PEA", DisplayName = "Property, Engineering & Agriculture", Parent = "NL" },
                    new LineOfBusiness { SystemName = "ONL", DisplayName = "Other Non-Life", Parent = "NL" },
                    new LineOfBusiness { SystemName = "ANN", DisplayName = "Annuity", Parent = "LI" },
                    new LineOfBusiness { SystemName = "DIS", DisplayName = "Disability", Parent = "LI" },
                    new LineOfBusiness { SystemName = "END", DisplayName = "Endowment", Parent = "LI" },
                    new LineOfBusiness { SystemName = "HYB", DisplayName = "Hybrid", Parent = "LI" },
                    new LineOfBusiness { SystemName = "ULI", DisplayName = "Unit Linked", Parent = "LI" },
                    new LineOfBusiness { SystemName = "OLI", DisplayName = "Other Life", Parent = "LI" },
                }
            },
            {
                typeof(Profitability), new []
                {
                    new Profitability { SystemName = "O", DisplayName = "Onerous" },
                    new Profitability { SystemName = "P", DisplayName = "Profitable" },
                    new Profitability { SystemName = "U", DisplayName = "Undetermined" },
                }
            },
            {
                typeof(Novelty), new []
                {
                    new Novelty { SystemName = "I", DisplayName = "In Force", Order = 1 },
                    new Novelty { SystemName = "N", DisplayName = "New Business", Order = 10 },
                    new Novelty { SystemName = "C", DisplayName = "Combined", Order = 20 },
                }
            },
            { typeof(OciType), new []
                { new OciType { SystemName = "Default", DisplayName = "Default" }, } },
            {
                typeof(Partner), new []
                {
                    new Partner { SystemName = "PT1", DisplayName = "Partner1" },
                    new Partner { SystemName = "PTI", DisplayName = "Internal Partner" }, }
            },
            {
                typeof(BsVariableType), new [] { new BsVariableType { SystemName = "D", DisplayName = "Changes in Balance", Order = 10 }, }
            },
            {
                typeof(PnlVariableType), new []
                {
                    new PnlVariableType { SystemName = "TCI", DisplayName = "Total Comprehensive Income", Order = 0 },
                    new PnlVariableType { SystemName = "PNL", DisplayName = "Profit and Loss", Order = 100 },
                    new PnlVariableType { SystemName = "OCI", DisplayName = "Other Comprehensive Income", Order = 200 },
                    new PnlVariableType { SystemName = "ISR", DisplayName = "Insurance Service Result", Order = 300 },
                    new PnlVariableType { SystemName = "IR", DisplayName = "Insurance Revenue", Order = 400 },
                    new PnlVariableType { SystemName = "ISE", DisplayName = "Insurance Service Expense", Order = 500 },
                    new PnlVariableType { SystemName = "IFIE", DisplayName = "Insurance Finance Income/Expense", Order = 600 },
                    new PnlVariableType { SystemName = "IR1", DisplayName = "Premiums", Order = 401 },
                    new PnlVariableType { SystemName = "IR2", DisplayName = "Exc. Investment Components", Order = 402 },
                    new PnlVariableType { SystemName = "IR3", DisplayName = "CSM Amortization", Order = 403 },
                    new PnlVariableType { SystemName = "IR4", DisplayName = "Acquistion Expenses Amortization", Order = 404 },
                    new PnlVariableType { SystemName = "IR5", DisplayName = "Non-Financial LRC/LC Changes (Exc. CSM Amortization)", Order = 405 },
                    new PnlVariableType { SystemName = "IR6", DisplayName = "Exc. Experience Adjustments", Order = 406 },
                    new PnlVariableType { SystemName = "IR7", DisplayName = "On Premiums", Order = 407 },
                    new PnlVariableType { SystemName = "IR77", DisplayName = "Total", Order = 408 },
                    new PnlVariableType { SystemName = "IR78", DisplayName = "To CSM", Order = 409 },
                    new PnlVariableType { SystemName = "IR79", DisplayName = "To Financial Performance", Order = 410 },
                    new PnlVariableType { SystemName = "IR8", DisplayName = "On Acquistion Expenses", Order = 411 },
                    new PnlVariableType { SystemName = "IR9", DisplayName = "Exc. Financial on Deferrals", Order = 415 },
                    new PnlVariableType { SystemName = "IR11", DisplayName = "PAA Earned Premiums", Order = 451 },
                    new PnlVariableType { SystemName = "IR12", DisplayName = "Experience Adjustment on Premiums", Order = 452 },
                    new PnlVariableType { SystemName = "IR13", DisplayName = "Expected Releases / Amortizations", Order = 453 },
                    new PnlVariableType { SystemName = "IR14", DisplayName = "FCF Locked-In Interest Rate Correction", Order = 454 },
                    new PnlVariableType { SystemName = "ISE1", DisplayName = "Reinsurance Premiums", Order = 501 },
                    new PnlVariableType { SystemName = "ISE2", DisplayName = "Claims", Order = 502 },
                    new PnlVariableType { SystemName = "ISE3", DisplayName = "Expenses", Order = 503 },
                    new PnlVariableType { SystemName = "ISE4", DisplayName = "Commissions", Order = 504 },
                    new PnlVariableType { SystemName = "ISE41", DisplayName = "Claim Expenses", Order = 505 },
                    new PnlVariableType { SystemName = "ISE5", DisplayName = "Exc. Investment Components", Order = 506 },
                    new PnlVariableType { SystemName = "ISE6", DisplayName = "Acquisition Expenses", Order = 507 },
                    new PnlVariableType { SystemName = "ISE7", DisplayName = "Reinsurance CSM Amortization", Order = 508 },
                    new PnlVariableType { SystemName = "ISE8", DisplayName = "LoReCo Release", Order = 509 },
                    new PnlVariableType { SystemName = "ISE9", DisplayName = "Loss Component Release", Order = 510 },
                    new PnlVariableType { SystemName = "ISE10", DisplayName = "Non-Financial Reinsurance LRC Changes (Exc. LC/LoReCo)", Order = 511 },
                    new PnlVariableType { SystemName = "ISE11", DisplayName = "Loss Component / LoReCo Changes (Exc. Releases)", Order = 512 },
                    new PnlVariableType { SystemName = "ISE12", DisplayName = "Non Financial LIC Changes", Order = 513 },
                    new PnlVariableType { SystemName = "ISE13", DisplayName = "Exc. Financial on Deferrals", Order = 515 },
                    new PnlVariableType { SystemName = "ISE20", DisplayName = "Reinsurance Revenue", Order = 520 },
                    new PnlVariableType { SystemName = "ISE201", DisplayName = "PAA Earned Premiums", Order = 521 },
                    new PnlVariableType { SystemName = "ISE21", DisplayName = "Experience Adjustment on Premiums", Order = 522 },
                    new PnlVariableType { SystemName = "ISE22", DisplayName = "Expected Releases / Amortizations (Exc. LoReCo)", Order = 523 },
                    new PnlVariableType { SystemName = "ISE23", DisplayName = "Exc. Investment Components", Order = 524 },
                    new PnlVariableType { SystemName = "ISE24", DisplayName = "FCF Locked-In Interest Rate Correction", Order = 525 },
                    new PnlVariableType { SystemName = "ISE25", DisplayName = "Exc. Financial on Deferrals", Order = 526 },
                    new PnlVariableType { SystemName = "IFIE1", DisplayName = "Financial LRC/LC Changes", Order = 601 },
                    new PnlVariableType { SystemName = "IFIE2", DisplayName = "Financial LIC Changes", Order = 602 },
                    new PnlVariableType { SystemName = "IFIE3", DisplayName = "FX Changes", Order = 603 },
                    new PnlVariableType { SystemName = "OCI1", DisplayName = "Financial LRC Changes", Order = 201 },
                    new PnlVariableType { SystemName = "OCI2", DisplayName = "Financial LIC Changes", Order = 202 },
                    new PnlVariableType { SystemName = "OCI3", DisplayName = "FX Changes", Order = 203 },
                }
            },
            {
                typeof(RiskDriver),
                new [] { new RiskDriver  { SystemName = "Default", DisplayName = "Default" } }
            },
            {
                typeof(Scenario), new []
                {
                    new Scenario { SystemName = "YCUP1.0pct", DisplayName = "Yield Curve Up 1.0pct" },
                    new Scenario { SystemName = "YCDW1.0pct", DisplayName = "Yield Curve Down 1.0pct" },
                    new Scenario { SystemName = "SRUP1.0pct", DisplayName = "Spread Rate Up 1.0pct" },
                    new Scenario { SystemName = "SRDW1.0pct", DisplayName = "Spread Rate Down 1.0pct" },
                    new Scenario { SystemName = "EUP1.0pct", DisplayName = "Equity Up 1.0pct" },
                    new Scenario { SystemName = "EDW1.0pct", DisplayName = "Equity Down 1.0pct" },
                    new Scenario { SystemName = "FXUP1.0pct", DisplayName = "Exchange Rate Up 1.0pct" },
                    new Scenario { SystemName = "FXDW1.0pct", DisplayName = "Exchange Rate Down 1.0pct" },
                    new Scenario { SystemName = "MTUP10pct", DisplayName = "Mortality Up 10pct" },
                    new Scenario { SystemName = "MTDW10pct", DisplayName = "Mortality Down 10pct" },
                    new Scenario { SystemName = "LUP10pct", DisplayName = "Longevity Up 10pct" },
                    new Scenario { SystemName = "LDW10pct", DisplayName = "Longevity Down 10pct" },
                    new Scenario { SystemName = "DUP10pct", DisplayName = "Disability Up 10pct" },
                    new Scenario { SystemName = "DDW10pct", DisplayName = "Disability Down 10pct" },
                    new Scenario { SystemName = "LICUP10pct", DisplayName = "Lic Up 10pct" },
                    new Scenario { SystemName = "LICDW10pct", DisplayName = "Lic Down 10pct" }
                }
            },
            {
                typeof(ValuationApproach), new []
                {
                    new ValuationApproach { SystemName = "BBA", DisplayName = "Building Block Approach" },
                    new ValuationApproach { SystemName = "PAA", DisplayName = "Premium Allocation Approach" }
                }
            },
            {
                typeof(ProjectionConfiguration), new []
                {
                    new ProjectionConfiguration { SystemName = "P0", DisplayName = "End of January", Shift = 0, TimeStep = 1, Order = 10 },
                    new ProjectionConfiguration { SystemName = "P1", DisplayName = "End of February", Shift = 0, TimeStep = 2, Order = 20 },
                    new ProjectionConfiguration { SystemName = "P2", DisplayName = "End of March", Shift = 0, TimeStep = 3, Order = 30 },
                    new ProjectionConfiguration { SystemName = "P3", DisplayName = "End of April", Shift = 0, TimeStep = 4, Order = 40 },
                    new ProjectionConfiguration { SystemName = "P4", DisplayName = "End of May", Shift = 0, TimeStep = 5, Order = 50 },
                    new ProjectionConfiguration { SystemName = "P5", DisplayName = "End of June", Shift = 0, TimeStep = 6, Order = 60 },
                    new ProjectionConfiguration { SystemName = "P6", DisplayName = "End of July", Shift = 0, TimeStep = 7, Order = 70 },
                    new ProjectionConfiguration { SystemName = "P7", DisplayName = "End of August", Shift = 0, TimeStep = 8, Order = 80 },
                    new ProjectionConfiguration { SystemName = "P8", DisplayName = "End of September", Shift = 0, TimeStep = 9, Order = 90 },
                    new ProjectionConfiguration { SystemName = "P9", DisplayName = "End of October", Shift = 0, TimeStep = 10, Order = 100 },
                    new ProjectionConfiguration { SystemName = "P10", DisplayName = "End of November", Shift = 0, TimeStep = 11, Order = 110 },
                    new ProjectionConfiguration { SystemName = "P11", DisplayName = "End of December", Shift = 0, TimeStep = 12, Order = 120 },
                    new ProjectionConfiguration { SystemName = "P12", DisplayName = "End of Year+1", Shift = 12, TimeStep = 12, Order = 130 },
                    new ProjectionConfiguration { SystemName = "P13", DisplayName = "End of Year+2", Shift = 24, TimeStep = 12, Order = 140 },
                    new ProjectionConfiguration { SystemName = "P14", DisplayName = "End of Year+3", Shift = 36, TimeStep = 12, Order = 150 },
                    new ProjectionConfiguration { SystemName = "P15", DisplayName = "End of Year+4", Shift = 48, TimeStep = 12, Order = 160 },
                    new ProjectionConfiguration { SystemName = "P16", DisplayName = "Year+5 to Year+10", Shift = 60, TimeStep = 60, Order = 170 },
                    new ProjectionConfiguration { SystemName = "P17", DisplayName = "Year+10 to Year+15", Shift = 120, TimeStep = 60, Order = 180 },
                    new ProjectionConfiguration { SystemName = "P18", DisplayName = "Year+15 to Year+20", Shift = 180, TimeStep = 60, Order = 190 },
                    new ProjectionConfiguration { SystemName = "P19", DisplayName = "Years Over +20", Shift = 240, TimeStep = 9999, Order = 200 },
                }
            },
        };
}