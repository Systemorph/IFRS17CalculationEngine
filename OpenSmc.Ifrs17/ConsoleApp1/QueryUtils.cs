using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;

namespace OpenSms.Ifrs17.CalculationScopes;

public static class QueryUtils
{
    public static double[] GetValues(this IEnumerable<RawVariable> rv, ImportIdentity id, Func<RawVariable, bool> whereClause) => rv
        .Where(v => v.AocType == id.AocType &&
                    v.Novelty == id.Novelty &&
                    whereClause(v) &&
                    v.DataNode == id.DataNode)
        .Select(v => v?.Values ?? null).AggregateDoubleArray();

    public static double GetValue(this IEnumerable<IfrsVariable> ifrsv, ImportIdentity id, Func<IfrsVariable, bool> whereClause, int projection = 0) => ifrsv
        .Where(v => v.AocType == id.AocType &&
                    v.Novelty == id.Novelty &&
                    whereClause(v) && v.DataNode == id.DataNode
        )
        .Select(v => v?.Values ?? null).AggregateDoubleArray().ElementAtOrDefault(projection);

    public static ProjectionConfiguration[] SortRelevantProjections(this ProjectionConfiguration[] pc, int month)
    {
        return pc.Where(x => x.Shift > 0 || x.TimeStep == month || x.TimeStep > month && x.TimeStep % Consts.MonthInAQuarter == 0)
            .OrderBy(x => x.Shift)
            .ThenBy(x => x.TimeStep)
            .ToArray();
    }
}