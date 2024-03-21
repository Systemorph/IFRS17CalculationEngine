using OpenSmc.Collections;
using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.Args;
using OpenSmc.Ifrs17.DataTypes.DataModel.Interfaces;
using System.Linq.Expressions;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Hierarchies;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.Utils;

public static class Queries
{
    public static IEnumerable<AocConfiguration> LoadAocStepConfiguration(this IWorkspace workspace, int year, int month)
        => workspace.LoadParameter<AocConfiguration>(year, month)
            .GroupBy(x => (x.AocType, x.Novelty),
            (k, v) => v.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).First());

    public static Dictionary<string, HashSet<string>> GetAmountTypesByEstimateType(IHierarchicalDimensionCache hierarchyCache)
    {
        return new Dictionary<string, HashSet<string>>(){
                {EstimateTypes.RA, new string[]{}.ToHashSet()},
                {EstimateTypes.C, new string[]{}.ToHashSet()},
                {EstimateTypes.L, new string[]{}.ToHashSet()},
                {EstimateTypes.LR, new string[]{}.ToHashSet()},
            };
    }
    public static HashSet<string> GetTechnicalMarginEstimateType()
    {
        return new[] { EstimateTypes.C, EstimateTypes.L, EstimateTypes.LR, }.ToHashSet();
    }

    public static Dictionary<string, DataNodeData> LoadDataNodes(this IWorkspace querySource, Args args)
    {
        var dataNodeStates = querySource.LoadCurrentAndPreviousParameter<DataNodeState>(args, x => x.DataNode);
        var activeDataNodes = dataNodeStates.Where(kvp => kvp.Value[Consts.CurrentPeriod].State != State.Inactive).Select(kvp => kvp.Key);

        return querySource.GetData<GroupOfContract>().Where(dn => activeDataNodes.Contains(dn.SystemName)).ToArray()
            .ToDictionary(dn => dn.SystemName, dn => {
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
    public static Dictionary<string, DataNodeState> LoadDataNodeState(this IWorkspace querySource, Args args)
    {
        return querySource.LoadCurrentAndPreviousParameter<DataNodeState>(args, x => x.DataNode)
            .Where(x => x.Value[Consts.CurrentPeriod].State != State.Inactive)
            .ToDictionary(x => x.Key, x => x.Value[Consts.CurrentPeriod]);
    }

    public static Dictionary<string, Dictionary<int, T>> LoadCurrentAndPreviousParameter<T>(
        this IWorkspace querySource,
        Args args,
        Func<T, string> identityExpression,
        Expression<Func<T, bool>> filterExpression = null)
        where T : class, IWithYearMonthAndScenario
    {
        var parameters = querySource.LoadParameter<T>(args.Year, args.Month, filterExpression)
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
            inner.Add(Consts.PreviousPeriod, previousCandidate != null ? previousCandidate : (currentCandidateBE != null ? currentCandidateBE : currentCandidate));
            // TODO: log error if currentCandidate is null
        }
        return ret;
    }

    public static Dictionary<string, T> LoadCurrentParameter<T>(
        this IWorkspace querySource,
        Args args,
        Func<T, string> identityExpression,
        Expression<Func<T, bool>> filterExpression = null)
        where T : class, IWithYearMonthAndScenario
    {
        return querySource.LoadParameter<T>(args.Year, args.Month, filterExpression)
            .Where(x => x.Scenario == args.Scenario || x.Scenario == null)
            .GroupBy(identityExpression)
            .Select(x => x.OrderByDescending(y => y.Year)
                .ThenByDescending(y => y.Month)
                .ThenByDescending(y => y.Scenario)
                .FirstOrDefault())
            .ToDictionary(identityExpression);
    }

    public static T[] LoadParameter<T>(this IWorkspace workspace, int year, int month, Expression<Func<T, bool>> filterExpression = null)
        where T : class, IWithYearAndMonth
    {
        return workspace.GetData<T>()
            .Where(x => x.Year == year && x.Month <= month || x.Year < year)
            //.Where(filterExpression?? (x => true))
            .ToArray();
    }

    public static Dictionary<string, Dictionary<int, SingleDataNodeParameter>> LoadSingleDataNodeParameters(this IWorkspace querySource, Args args)
    {
        return querySource.LoadCurrentAndPreviousParameter<SingleDataNodeParameter>(args, x => x.DataNode);
    }
}
