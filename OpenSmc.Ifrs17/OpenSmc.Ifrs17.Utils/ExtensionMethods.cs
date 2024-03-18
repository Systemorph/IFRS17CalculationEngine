using OpenSmc.Data;
using OpenSmc.DataCubes;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.Utils;

public static class ExtensionMethods
{
    public static TDataSource ConfigureCategory<TDataSource>(this TDataSource dataSource, IDictionary<Type, IEnumerable<object>> typeAndInstance)
        where TDataSource : DataSource<TDataSource>
        => typeAndInstance.Aggregate(dataSource, (ds, kvp) => ds.WithType(kvp.Key, t => t.WithInitialData(kvp.Value)));

    public static Dictionary<TKey, TResult> ToDictionaryGrouped<TSource, TKey, TResult>
        (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<IGrouping<TKey, TSource>, TResult> elementSelector)
        => source.GroupBy(keySelector).ToDictionary(g => g.Key, elementSelector);

    public static IDataCube<TTarget> SelectToDataCube<TSource, TTarget>
        (this IEnumerable<TSource> source, Func<TSource, bool> whereClause, Func<TSource, TTarget> selector)
        => source.Where(whereClause).Select(selector).ToDataCube();

    public static IDataCube<TTarget> SelectToDataCube<TSource, TTarget>
        (this IEnumerable<TSource> source, Func<TSource, TTarget> selector)
        => source.SelectToDataCube(x => true, selector);

    public static ProjectionConfiguration[] SortRelevantProjections(this ProjectionConfiguration[] pc, int month)
        => pc.Where(x => x.Shift > 0 || x.TimeStep == month || (x.TimeStep > month && x.TimeStep % BusinessConstant.MonthInAQuarter == 0))
             .OrderBy(x => x.Shift)
             .ThenBy(x => x.TimeStep)
             .ToArray();
}

