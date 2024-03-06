using System.Linq.Expressions;
using OpenSmc.Collections;
using OpenSmc.Data;
using OpenSmc.Data.Persistence;
using OpenSmc.DataCubes;
using OpenSmc.Equality;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.Interfaces;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;

namespace OpenSmc.Ifrs17.ReportHub;

public static class ReportStorageExtensions
{
    public static Dictionary<TKey, TResult> ToDictionaryGrouped<TSource, TKey, TResult>
        (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<IGrouping<TKey, TSource>, TResult> elementSelector) 
        => source.GroupBy(keySelector).ToDictionary(g => g.Key, elementSelector);
    
    public static IDataCube<TTarget> SelectToDataCube<TSource, TTarget>
        (this IEnumerable<TSource> source, Func<TSource, bool> whereClause, Func<TSource, TTarget> selector) 
        => source.Where(whereClause).Select(selector).ToDataCube();

    public static IDataCube<TTarget> SelectToDataCube<TSource, TTarget>
        (this IEnumerable<TSource> source, Func<TSource, TTarget> selector) 
        => source.SelectToDataCube(x => true, selector);

    public static ProjectionConfiguration[] SortRelevantProjections(this ProjectionConfiguration[] pc, int month) =>
        pc.Where(x => x.Shift > 0 || x.TimeStep == month || (x.TimeStep > month && x.TimeStep % BusinessConstant.MonthInAQuarter == 0))
            .OrderBy(x => x.Shift)
            .ThenBy(x => x.TimeStep)
            .ToArray();

    public static Dictionary<string, Dictionary<FxPeriod, double>> GetExchangeRatesDictionary(this HubDataSource workspace, int year, int month)
        => (workspace.Get<ExchangeRate>()
                .Where(x => x.Year == year - 1 && x.Month == BusinessConstant.MonthInAYear && x.FxType == FxType.Spot ||
                            x.Year == year && x.Month == month)
                .ToArray())
            .ToDictionaryGrouped(x => x.Currency,
                x => x.ToDictionary(y => (y.Year, y.Month, y.FxType) switch
                    {
                        (_, _, _) when y.Year == year - 1 && y.Month == BusinessConstant.MonthInAYear && y.FxType == FxType.Spot => FxPeriod.BeginningOfPeriod,
                        (_, _, _) when y.Year == year && y.Month == month && y.FxType == FxType.Average => FxPeriod.Average,
                        (_, _, _) when y.Year == year && y.Month == month && y.FxType == FxType.Spot => FxPeriod.EndOfPeriod
                    },
                    y => y.FxToGroupCurrency));

    public static IEnumerable<AocConfiguration> LoadAocStepConfiguration(this HubDataSource workspace, int year, int month)
        => workspace.LoadParameter<AocConfiguration>(year, month)
            .GroupBy(x => (x.AocType, x.Novelty),
                (k, v) => v.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).First());

    public static T[] LoadParameter<T>(this HubDataSource workspace, int year, int month, Func<T, bool>? filterExpression = null)
        where T : class, IWithYearAndMonth
    {
        return workspace.Get<T>()
            .Where(x => x.Year == year && x.Month <= month || x.Year < year)
            .Where(filterExpression ?? (x => true))
            .ToArray();
    }

    public static ICollection<ReportVariable> QueryReportVariables(this HubDataSource workspace, (int Year, int Month, string ReportingNode, string Scenario) args, ProjectionConfiguration[] orderedProjectionConfigurations)
    {
        var bestEstimate = workspace.QueryReportVariablesSingleScenario((args.Year, args.Month, args.ReportingNode, null), orderedProjectionConfigurations);
        return (args.Scenario == null)
            ? bestEstimate
            : workspace.QueryReportVariablesSingleScenario((args.Year, args.Month, args.ReportingNode, args.Scenario), orderedProjectionConfigurations)
            .Union(bestEstimate.Select(x => x with { Scenario = args.Scenario }), EqualityComparer<ReportVariable>.Instance).ToArray();
    }

    public static ReportVariable[] QueryReportVariablesSingleScenario(this HubDataSource workspace, (int Year, int Month, string ReportingNode, string Scenario) args,
        ProjectionConfiguration[] orderedProjectionConfigurations)
    {
        //await workspace.Partition.SetAsync<PartitionByReportingNode>(new { ReportingNode = args.ReportingNode, Scenario = (string)null });
        //await workspace.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(new { ReportingNode = args.ReportingNode, Scenario = args.Scenario, Year = args.Year, Month = args.Month });
        var reportVariables = workspace.Get<GroupOfContract>()
                .Join(workspace.Get<IfrsVariable>(),
                    dn => dn.SystemName,
                    iv => iv.DataNode,
                    (dn, iv) => GetReportVariable(dn, iv, args, orderedProjectionConfigurations)
                )
                .ToArray()
            .SelectMany(rv => rv).ToArray();

        //await workspace.Partition.SetAsync<PartitionByReportingNode>(null);
        //await workspace.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(null);
        return reportVariables;
    }

    public static IEnumerable<ReportVariable> GetReportVariable(GroupOfContract goc, IfrsVariable iv, (int Year, int Month, string ReportingNode, string Scenario) args, ProjectionConfiguration[] orderedProjectionConfigurations) 
        => iv.Values.Select((val, ind) => new ReportVariable
    {
        ReportingNode = args.ReportingNode,
        Scenario = args.Scenario,
        Portfolio = goc.Portfolio,
        GroupOfContract = goc.SystemName,
        FunctionalCurrency = goc.FunctionalCurrency,
        ContractualCurrency = goc.ContractualCurrency,
        ValuationApproach = goc.ValuationApproach,
        OciType = goc.OciType,
        InitialProfitability = goc.Profitability,
        LiabilityType = goc.LiabilityType,
        AnnualCohort = goc.AnnualCohort,
        LineOfBusiness = goc.LineOfBusiness,
        IsReinsurance = goc is GroupOfReinsuranceContract,
        Partner = goc.Partner,
        EstimateType = iv.EstimateType,
        VariableType = iv.AocType,
        Novelty = iv.Novelty,
        AmountType = iv.AmountType,
        EconomicBasis = iv.EconomicBasis,
        AccidentYear = goc.LiabilityType == LiabilityTypes.LIC && iv.AccidentYear.HasValue
                                                            ? iv.AccidentYear.Value
                                                            : default,
        ServicePeriod = goc.LiabilityType == LiabilityTypes.LIC && iv.AccidentYear.HasValue
                                                            ? iv.AccidentYear == args.Year ? ServicePeriod.CurrentService : ServicePeriod.PastService
                                                            : ServicePeriod.NotApplicable,
        Projection = orderedProjectionConfigurations.ElementAtOrDefault(ind).SystemName,
        Value = val
    });

    public static double GetCurrencyToGroupFx(Dictionary<string, Dictionary<FxPeriod, double>> exchangeRates, string currency, FxPeriod fxPeriod, string groupCurrency)
    {
        if(currency == groupCurrency)
          return 1;
    
        if (!exchangeRates.TryGetValue(currency, out var currencyToGroup))
            //ApplicationMessage.Log(Error.ExchangeRateCurrency, currency);
            throw new Exception();
        if (!currencyToGroup.TryGetValue(fxPeriod, out var currencyToGroupFx))
            //ApplicationMessage.Log(Error.ExchangeRateNotFound, currency, fxPeriod.ToString());
            throw new Exception();
        return currencyToGroupFx;
    }
}

class EqualityComparer<T> : IEqualityComparer<T>
{
    private static readonly System.Reflection.PropertyInfo[] IdentityProperties = typeof(T).GetIdentityProperties().ToArray();
    private static Func<T, T, bool> compiledEqualityFunction;

    private EqualityComparer()
    {
        compiledEqualityFunction = GetEqualityFunction();
    }

    public static readonly EqualityComparer<T> Instance = new EqualityComparer<T>();

    public bool Equals(T x, T y) => compiledEqualityFunction(x, y);
    public int GetHashCode(T obj) => 0;

    private static Func<T, T, bool> GetEqualityFunction()
    {
        var prm1 = Expression.Parameter(typeof(T));
        var prm2 = Expression.Parameter(typeof(T));

        // r1 == null && r2 == null
        var nullConditionExpression = Expression.AndAlso(Expression.Equal(prm1, Expression.Constant(null, typeof(T))), Expression.Equal(prm2, Expression.Constant(null, typeof(T))));
        // r1 != null && r2 != null
        var nonNullConditionExpression = Expression.AndAlso(Expression.NotEqual(prm1, Expression.Constant(null, typeof(T))), Expression.NotEqual(prm2, Expression.Constant(null, typeof(T))));
        // r1.prop1 == r2.prop1 && r1.prop2 == r2.prop2...... 
        var allPropertiesEqualExpression = IdentityProperties.Select(propertyInfo => Expression.Equal(Expression.Property(prm1, propertyInfo), Expression.Property(prm2, propertyInfo))).Aggregate(Expression.AndAlso);

        var equalityExpr = Expression.OrElse(nullConditionExpression, Expression.AndAlso(nonNullConditionExpression, allPropertiesEqualExpression));
        return Expression.Lambda<Func<T, T, bool>>(equalityExpr, prm1, prm2).Compile();
    }
}