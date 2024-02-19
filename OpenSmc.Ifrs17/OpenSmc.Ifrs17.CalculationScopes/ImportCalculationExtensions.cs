using OpenSmc.Data;
using OpenSmc.DataStructures;
using OpenSmc.Hierarchies;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.Args;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;

namespace OpenSmc.Ifrs17.CalculationScopes;

public static class ImportCalculationExtensions
{
    public static double[] ComputeDiscountAndCumulate(this double[]? nominalValues, double[] monthlyDiscounting,
        PeriodType periodType)
    {
        if (nominalValues == null) return Enumerable.Empty<double>().ToArray();

        var ret = new double[nominalValues.Length];

        if (periodType == PeriodType.BeginningOfPeriod)
        {
            for (var i = nominalValues.Length - 1; i >= 0; i--)
                ret[i] = nominalValues[i] + ret.GetValidElement(i + 1) * monthlyDiscounting.GetValidElement(i / 12);
            return ret;
        }

        for (var i = nominalValues.Length - 1; i >= 0; i--)
            ret[i] = (nominalValues[i] + ret.GetValidElement(i + 1)) * monthlyDiscounting.GetValidElement(i / 12);

        return ret;
    }


    public static double[] ComputeDiscountAndCumulateWithCreditDefaultRisk(this double[] nominalValues,
        double[] monthlyDiscounting,
        double nonPerformanceRiskRate) //NonPerformanceRiskRate consider to be constant in time. Refinement would it be an array that takes as input tau/t.
    {
        if (!monthlyDiscounting.Any())
            monthlyDiscounting = new[] { 1d }; //Empty discounting array triggers Cumulation.
        return Enumerable.Range(0, nominalValues.Length)
            .Select(t => Enumerable.Range(t, nominalValues.Length - t)
                .Select(tau =>
                    nominalValues[tau] * Math.Pow(monthlyDiscounting.GetValidElement(t / 12), tau - t + 1) *
                    (Math.Exp(-nonPerformanceRiskRate * (tau - t)) - 1))
                .Sum())
            .ToArray();
    }


    //public static IDataCube<RawVariable> ComputeDiscountAndCumulate(this IDataCube<RawVariable>? nominalRawVariables,
    //    double[] yearlyDiscountRates, AmountType[] amountTypes)
    //{
    //    if (nominalRawVariables == null) return Enumerable.Empty<RawVariable>().ToDataCube();
    //    Dictionary<string?, PeriodType> periodTypeByAmountType = amountTypes.ToDictionary(x => x.SystemName, x => x.PeriodType);

    //    return nominalRawVariables.Select(rv =>
    //        {
    //            var values = rv.Values.ToArray();
    //            var cdcf = new double[values.Length];
    //            periodTypeByAmountType.TryGetValue(rv.AmountType, out var period);

    //            if (period == PeriodType.BeginningOfPeriod)
    //            {
    //                for (var i = cdcf.Length - 1; i >= 0; i--)
    //                    cdcf[i] = values[i] + cdcf.GetValidElement(i + 1) * yearlyDiscountRates.GetValidElement(i / 12);
    //            }
    //            else
    //            {
    //                for (var i = cdcf.Length - 1; i >= 0; i--)
    //                    cdcf[i] = (values[i] + cdcf.GetValidElement(i + 1)) *
    //                              yearlyDiscountRates.GetValidElement(i / 12);
    //            }

    //            return rv with { Values = cdcf };
    //        })
    //        .ToDataCube();
    //}


    public static double NewBusinessInterestAccretion(this IEnumerable<double> values,
        ICollection<double> monthlyInterestFactor, int timeStep, int shift)
    {
        var periodInterestAccretionFactors = Enumerable.Range(0, timeStep).Select(initialMonth => Enumerable
            .Range(initialMonth, timeStep - initialMonth)
            .Select(month => monthlyInterestFactor.GetValidElement(month / 12)).Aggregate(1d, (x, y) => x * y) - 1d);
        return values.Take(timeStep)
            .Zip(periodInterestAccretionFactors, (nominal, interestFactor) => nominal * interestFactor).Sum();
    }


    public static int GetProjections(IEnumerable<RawVariable> rawVariables,
        IEnumerable<IfrsVariable> ifrsVariables,
        string importFormat,
        ProjectionConfiguration[] projectionConfiguration)
    {

        if (Projection.EnableWithCutoff)
            return Projection.Cutoff + 1;

        if (!Projection.Enable)
            return 1;

        var iEnumerable = ifrsVariables as IfrsVariable[] ?? ifrsVariables.ToArray();
        var valueFromIfrsVariable =
            iEnumerable.Any() ? iEnumerable.Max(ifrsVariable => ifrsVariable.Values.Length) : 1;
        if (importFormat != ImportFormats.Cashflow)
            return valueFromIfrsVariable;

        var enumerable = rawVariables as RawVariable[] ?? rawVariables.ToArray();
        int rawVariableMaxLength = enumerable.Any() ? enumerable.Max(rawVariable => rawVariable.Values.Length) : 1;
        int valueFromRawVariable =
            projectionConfiguration.Where(projConfig => projConfig.Shift < rawVariableMaxLength).Count();
        return Math.Max(valueFromRawVariable, valueFromIfrsVariable);
    }


    public static InsurancePortfolio ExtendPortfolio(InsurancePortfolio pf, IDataRow datarow) => pf;


    public static ReinsurancePortfolio ExtendPortfolio(ReinsurancePortfolio pf, IDataRow datarow) => pf;


    public static GroupOfInsuranceContract ExtendGroupOfContract(GroupOfInsuranceContract gic, IDataRow datarow) => gic;


    public static GroupOfReinsuranceContract ExtendGroupOfContract(GroupOfReinsuranceContract gric, IDataRow datarow) => gric;


    public static SingleDataNodeParameter ExtendSingleDataNodeParameter(SingleDataNodeParameter singleDataNodeParameter,
        IDataRow datarow) => singleDataNodeParameter;


    public static string GetDefaultEconomicBasisDriver(string valuationApproach, string liabilityType)
    {
        return (valuationApproach, liabilityType) switch
        {
            (ValuationApproaches.BBA, _) => EconomicBases.L,
            (ValuationApproaches.VFA, _) => EconomicBases.C,
            (ValuationApproaches.PAA, LiabilityTypes.LIC) => EconomicBases.C,
            _ => EconomicBases.N,
        };
    }


    public static double[] Interpolate(this double[] cashflowValues, CashFlowPeriodicity periodicity,
        InterpolationMethod interpolationMethod)
    {
        if (periodicity == CashFlowPeriodicity.Monthly)
            return cashflowValues;

        var frequency = periodicity switch
        {
            CashFlowPeriodicity.Yearly => 12,
            CashFlowPeriodicity.Quarterly => 4,
            _ => 1
        };

        return interpolationMethod switch
        {
            InterpolationMethod.Start => cashflowValues
                .SelectMany(v => Enumerable.Range(0, frequency).Select(x => x == 0 ? v : default)).ToArray(),
            InterpolationMethod.Uniform or _ => cashflowValues
                .SelectMany(v => Enumerable.Range(0, frequency).Select(_ => v / frequency)).ToArray()
        };

    }


    public static int GetSign(string importFormat,
        (string AocType, string AmountType, string EstimateType, bool IsReinsurance) variable,
        IHierarchicalDimensionCache hierarchyCache) => 1;


    public static double[] AdjustValues(this double[] values, ImportArgs args, DataNodeData dataNodeData,
        int? AccidentYear) => values;


    public static async Task ExtendParsedVariables(this IWorkspace workspace,
        IHierarchicalDimensionCache hierarchyCache)
    {
    }


    public static Dictionary<string, HashSet<string>> GetAmountTypesByEstimateType(
        IHierarchicalDimensionCache hierarchyCache)
    {
        return new Dictionary<string, HashSet<string>>()
        {
            {EstimateTypes.RA, new string[] { }.ToHashSet()},
            {EstimateTypes.C, new string[] { }.ToHashSet()},
            {EstimateTypes.L, new string[] { }.ToHashSet()},
            {EstimateTypes.LR, new string[] { }.ToHashSet()},
        };
    }


    public static (string, string) ParseAmountAndEstimateType(this IDataRow datarow, string format,
        Dictionary<Type, Dictionary<string, string>> dimensionsWithExternalId,
        Dictionary<string, EstimateType> estimateTypes,
        Dictionary<string, AmountType> amountTypes)
    {
        return (datarow.Field<string>(nameof(RawVariable.AmountType)),
            datarow.Field<string>(nameof(RawVariable.EstimateType)));
    }


    public static HashSet<string> GetNonAttributableAmountTypes() => new string[] { AmountTypes.NE }.ToHashSet();


    public static HashSet<string> GetTechnicalMarginEstimateType()
    {
        return new[] { EstimateTypes.C, EstimateTypes.L, EstimateTypes.LR, }.ToHashSet();
    }


    public static HashSet<string> GetAocTypeWithoutCsmSwitch() =>
        new[] { AocTypes.BOP, AocTypes.EOP, AocTypes.AM, AocTypes.EA }.ToHashSet();


    public static class ComputationHelper
    {
        public static HashSet<string> ReinsuranceAocType = new[] { AocTypes.CRU, AocTypes.RCU }.ToHashSet();

        public static Dictionary<AocStep, IEnumerable<AocStep>> ReferenceAocSteps => new()
        {
            {new AocStep(AocTypes.EA, Novelties.C), new[] {new AocStep(AocTypes.CF, Novelties.C)}},
            {new AocStep(AocTypes.AM, Novelties.C), new[] {new AocStep(AocTypes.CL, Novelties.C)}},
            {new AocStep(AocTypes.EOP, Novelties.C), new[] {new AocStep(AocTypes.CL, Novelties.C)}},
        };

        public static Dictionary<string, string> ExperienceAdjustEstimateTypeMapping = new Dictionary<string, string>
            {{EstimateTypes.A, EstimateTypes.APA}, {EstimateTypes.BE, EstimateTypes.BEPA}};

        public static HashSet<DataType> CurrentPeriodCalculatedDataTypes =
            new[] { DataType.Calculated, DataType.CalculatedTelescopic }.ToHashSet();
    }


    public static double[] SetProjectionValue(double value, int period = 0) =>
        period == 0 || Math.Abs(value) > Consts.Precision
            ? Enumerable.Repeat(0d, period + 1).Select((y, i) => i == period ? value : y).ToArray()
            : null;
}


