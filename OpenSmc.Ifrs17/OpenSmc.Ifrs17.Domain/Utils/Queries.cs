using System.Linq.Expressions;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.Interfaces;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataSource.Api;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Workspace;


namespace OpenSmc.Ifrs17.Domain.Utils;

public static class Queries
{
    public static async Task<Dictionary<string, Dictionary<FxPeriod, double>>> GetExchangeRatesDictionaryAsync(this IQuerySource querySource, int year, int month)
    {
        return (await querySource.Query<ExchangeRate>()
                .Where(x => (x.Year == year - 1 && x.Month == Consts.MonthInAYear && x.FxType == FxType.Spot) ||
                            (x.Year == year && x.Month == month))
                .ToArrayAsync())
            .ToDictionaryGrouped(x => x.Currency,
                x => x.ToDictionary(y => (y.Year, y.Month, y.FxType) switch
                    {
                        (_, _, _) when y.Year == year - 1 && y.Month == Consts.MonthInAYear && y.FxType == FxType.Spot => FxPeriod.BeginningOfPeriod,
                        (_, _, _) when y.Year == year && y.Month == month && y.FxType == FxType.Average => FxPeriod.Average,
                        (_, _, _) when y.Year == year && y.Month == month && y.FxType == FxType.Spot => FxPeriod.EndOfPeriod
                    },
                    y => y.FxToGroupCurrency));
    }


    public static async Task<T[]> LoadParameterAsync<T>(
        this IQuerySource querySource,
        int year,
        int month,
        Expression<Func<T, bool>> filterExpression = null)
        where T : IWithYearAndMonth
    {
        return await querySource.Query<T>()
            .Where(x => (x.Year == year && x.Month <= month) || x.Year < year)
            .Where(filterExpression ?? (Expression<Func<T, bool>>)(x => true))
            .ToArrayAsync();
    }


    public static async Task<Dictionary<string, T>> LoadCurrentParameterAsync<T>(
        this IQuerySource querySource,
        Args args,
        Func<T, string> identityExpression,
        Expression<Func<T, bool>> filterExpression = null)
        where T : IWithYearMonthAndScenario
    {
        return (await querySource.LoadParameterAsync<T>(args.Year, args.Month, filterExpression))
            .Where(x => x.Scenario == args.Scenario || x.Scenario == null)
            .GroupBy(identityExpression)
            .Select(x => x.OrderByDescending(y => y.Year)
                .ThenByDescending(y => y.Month)
                .ThenByDescending(y => y.Scenario)
                .FirstOrDefault())
            .ToDictionary(identityExpression);
    }


    public static async Task<Dictionary<string, Dictionary<int, T>>> LoadCurrentAndPreviousParameterAsync<T>(
        this IQuerySource querySource,
        Args args,
        Func<T, string> identityExpression,
        Expression<Func<T, bool>> filterExpression = null)
        where T : IWithYearMonthAndScenario
    {
        var parameters = (await querySource.LoadParameterAsync<T>(args.Year, args.Month, filterExpression))
            .Where(yc => yc.Scenario == args.Scenario || yc.Scenario == null)
            .GroupBy(identityExpression);

        var ret = new Dictionary<string, Dictionary<int, T>>();
        foreach (var p in parameters)
        {
            var inner = ret.GetOrAdd(p.Key, _ => new Dictionary<int, T>());

            var currentCandidate = p.Where(x => x.Year == args.Year).OrderByDescending(x => x.Month).ThenByDescending(x => x.Scenario).FirstOrDefault();
            var previousCandidate = p.Where(x => x.Year < args.Year && x.Scenario == null).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).FirstOrDefault();
            var currentCandidateBE = p.Where(x => x.Year <= args.Year && x.Scenario == null).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).FirstOrDefault();

            inner.Add(Consts.CurrentPeriod, currentCandidate != null ? currentCandidate : previousCandidate);
            inner.Add(Consts.PreviousPeriod, previousCandidate != null ? previousCandidate : currentCandidateBE != null ? currentCandidateBE : currentCandidate);
            // TODO: log error if currentCandidate is null
        }

        return ret;
    }


    public static async Task<Dictionary<string, YieldCurve>> LoadLockedInYieldCurveAsync(this IQuerySource querySource, Args args, IEnumerable<DataNodeData> dataNodes)

    {
        var lockedInYieldCurveByGoc = new Dictionary<string, YieldCurve>();
        foreach (var dn in dataNodes.Where(x => x.ValuationApproach != ValuationApproaches.VFA))
        {
            var monthUpperLimit = args.Year == dn.Year ? args.Month : Consts.MonthInAYear;
            var argsNew = args with { Year = dn.Year, Month = monthUpperLimit, Scenario = args.Scenario };
            var loadedYc = await querySource.LoadCurrentParameterAsync<YieldCurve>(argsNew, x => x.Currency, x => x.Currency == dn.ContractualCurrency && x.Name == dn.YieldCurveName);

            lockedInYieldCurveByGoc[dn.DataNode] = loadedYc.TryGetValue(dn.ContractualCurrency, out var lockedYc) ? lockedYc : null;
        }

        return lockedInYieldCurveByGoc;
    }


    public static async Task<Dictionary<string, Dictionary<int, YieldCurve>>> LoadCurrentYieldCurveAsync(this IQuerySource querySource, Args args,
        IEnumerable<DataNodeData> dataNodes)
    {
        var currentYieldCurveByGoc = new Dictionary<string, Dictionary<int, YieldCurve>>();

        var dnByValAppContrCurrYcName = dataNodes.ToDictionaryGrouped(x => (ValuationApproach: x.ValuationApproach, ContractualCurrency: x.ContractualCurrency, YieldCurveName: x.YieldCurveName),
            x => x.Select(y => y.DataNode).ToArray());

        foreach (var key in dnByValAppContrCurrYcName.Keys)
        {
            var loadedYc = await querySource.LoadCurrentAndPreviousParameterAsync<YieldCurve>(args,
                x => x.Currency,
                x => x.Currency == key.ContractualCurrency
                     && (key.ValuationApproach == ValuationApproaches.VFA
                         ? x.Name == key.YieldCurveName
                         : x.Name == (string)null));

            foreach (var dn in dnByValAppContrCurrYcName[key])
                currentYieldCurveByGoc[dn] = loadedYc.TryGetValue(key.ContractualCurrency, out var currentYcDict) ? currentYcDict : null;
        }

        return currentYieldCurveByGoc;
    }


    public static async Task<Dictionary<string, DataNodeState>> LoadDataNodeStateAsync(this IQuerySource querySource, Args args)
    {
        return (await querySource.LoadCurrentAndPreviousParameterAsync<DataNodeState>(args, x => x.DataNode))
            .Where(x => x.Value[Consts.CurrentPeriod].State != State.Inactive)
            .ToDictionary(x => x.Key, x => x.Value[Consts.CurrentPeriod]);
    }


    public static async Task<Dictionary<string, DataNodeData>> LoadDataNodesAsync(this IQuerySource querySource, Args args)
    {
        var dataNodeStates = await querySource.LoadCurrentAndPreviousParameterAsync<DataNodeState>(args, x => x.DataNode);
        var activeDataNodes = dataNodeStates.Where(kvp => kvp.Value[Consts.CurrentPeriod].State != State.Inactive).Select(kvp => kvp.Key);

        return (await querySource.Query<GroupOfContract>().Where(dn => activeDataNodes.Contains(dn.SystemName)).ToArrayAsync())
            .ToDictionary(dn => dn.SystemName, dn =>
                {
                    var dnCurrentState = dataNodeStates[dn.SystemName][Consts.CurrentPeriod];
                    var dnPreviousState = dataNodeStates[dn.SystemName][Consts.PreviousPeriod];
                    return new DataNodeData(dn)
                    {
                        Year = dnPreviousState.Year,
                        Month = dnPreviousState.Month,
                        State = dnCurrentState.State,
                        PreviousState = dnPreviousState.State
                    };
                }
            );
    }


    public static async Task<Dictionary<string, Dictionary<int, SingleDataNodeParameter>>> LoadSingleDataNodeParametersAsync(this IQuerySource querySource, Args args)
    {
        return await querySource.LoadCurrentAndPreviousParameterAsync<SingleDataNodeParameter>(args, x => x.DataNode);
    }


    public static async Task<Dictionary<string, Dictionary<int, HashSet<InterDataNodeParameter>>>> LoadInterDataNodeParametersAsync(this IQuerySource querySource, Args args)
    {
        var identityExpressions = new Func<InterDataNodeParameter, string>[] { x => x.DataNode, x => x.LinkedDataNode };
        var parameterArray = await querySource.LoadParameterAsync<InterDataNodeParameter>(args.Year, args.Month);
        var parameters = identityExpressions.SelectMany(ie => parameterArray.GroupBy(ie));

        return parameters.SelectMany(p => p
                .GroupBy(x => x.DataNode != p.Key ? x.DataNode : x.LinkedDataNode)
                .Select(gg =>
                {
                    var currentCandidate = gg.Where(x => x.Year == args.Year).OrderByDescending(x => x.Month).ThenByDescending(x => x.Scenario).FirstOrDefault();
                    var previousCandidate = gg.Where(x => x.Year < args.Year && x.Scenario == null).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).FirstOrDefault();
                    return (key: p.Key,
                        currentPeriod: currentCandidate != null ? currentCandidate : previousCandidate,
                        previousPeriod: previousCandidate != null ? previousCandidate : currentCandidate);
                })
            )
            .ToDictionaryGrouped(x => x.key,
                x => new Dictionary<int, HashSet<InterDataNodeParameter>>
                {
                    { Consts.CurrentPeriod, x.Select(y => y.currentPeriod).ToHashSet() },
                    { Consts.PreviousPeriod, x.Select(y => y.previousPeriod).ToHashSet() }
                });
    }


    public static async Task<IEnumerable<AocConfiguration>> LoadAocStepConfigurationAsync(this IQuerySource querySource, int year, int month)
    {
        return (await querySource.LoadParameterAsync<AocConfiguration>(year, month))
            .GroupBy(x => (x.AocType, x.Novelty),
                (k, v) => v.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).First());
    }


    public static async Task<Dictionary<AocStep, AocConfiguration>> LoadAocStepConfigurationAsDictionaryAsync(this IQuerySource querySource, int year, int month)
    {
        return (await querySource.LoadAocStepConfigurationAsync(year, month))
            .ToDictionary(x => new AocStep(x.AocType, x.Novelty));
    }


    public static async Task<T[]> LoadPartitionedDataAsync<T, P>(this Systemorph.Vertex.DataSource.Common.IDataSource querySource, Guid partition)
        where T : IPartitioned
        where P : IPartition
    {
        var partitionBackup = (Guid)(querySource.Partition.GetCurrent(typeof(P).Name) ?? default(Guid));
        await querySource.Partition.SetAsync<P>(partition);
        // Temporary workaround for physical database: where clause is necessary
        var data = await querySource.Query<T>().Where(x => x.Partition == partition).ToArrayAsync();
        if (partitionBackup == default) await querySource.Partition.SetAsync<P>(null);
        else await querySource.Partition.SetAsync<P>(partitionBackup);
        return data;
    }


    public static async Task<T[]> QueryPartitionedDataAsync<T, P>(this IWorkspace workspace, IDataSource dataSource, Guid targetPartition, Guid defaultPartition, string format)
        where T : IPartitioned
        where P : IPartition
    {
        var isRelaxed = (format != ImportFormats.Cashflow && typeof(T).Name == nameof(IfrsVariable)) ||
                        (format == ImportFormats.Cashflow && typeof(T).Name == nameof(RawVariable));

        var variablesFromWorkspace = await workspace.LoadPartitionedDataAsync<T, P>(targetPartition);
        if (!isRelaxed || variablesFromWorkspace.Any()) return variablesFromWorkspace;

        // This is for scenario re-calculation
        var variablesFromDataSource = await dataSource.LoadPartitionedDataAsync<T, P>(targetPartition);
        if (variablesFromDataSource.Any()) return variablesFromDataSource;

        // This is for scenarios affecting parameters solely
        // And for the best estimate when parameters are updated
        return await dataSource.LoadPartitionedDataAsync<T, P>(defaultPartition);
    }
}