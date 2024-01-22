using System.Globalization;
using System.Linq.Expressions;

using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;

using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Equality;
//using static MoreLinq.Extensions.ZipLongestExtension;
//using MoreLinq.Extensions.ZipLongestExtension;

namespace OpenSmc.Ifrs17.Domain.Utils;

public static class Utils
{
    public static T? GetValidElement<T>(this ICollection<T> collection, int index)
    {
        var count = collection.Count;
        if (collection == null || count == 0)
            return default;

        if (index < 0)
        {
            ApplicationMessage.Log(Error.NegativeIndex);
            return default;
        }

        return index < count
            ? collection.ElementAt(index)
            : collection.ElementAt(count - 1);
    }


    public static Dictionary<TKey, TResult> ToDictionaryGrouped<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<IGrouping<TKey, TSource>, TResult> elementSelector)
    {
        return source.GroupBy(keySelector).ToDictionary(g => g.Key, elementSelector);
    }


    public static IDataCube<TTarget> SelectToDataCube<TSource, TTarget>(this IEnumerable<TSource> source, Func<TSource, bool> whereClause, Func<TSource, TTarget> selector)
    {
        return source.Where(whereClause).Select(selector).ToDataCube();
    }


    public static IDataCube<TTarget> SelectToDataCube<TSource, TTarget>(this IEnumerable<TSource> source, Func<TSource, TTarget> selector)
    {
        return source.SelectToDataCube(x => true, selector);
    }


    public static double[] Prune(this IEnumerable<double> source, double precision = Consts.Precision)
    {
        return source.Reverse().SkipWhile(x => Math.Abs(x) < precision).Reverse().ToArray();
    }


    public static double[] PruneButFirst(this IEnumerable<double> source, double precision = Consts.Precision)
    {
        var pruned = source.Prune(precision);
        if (pruned.Count() < source.Count())
            return pruned.Concat(new[] { (double)default }).ToArray();
        return pruned;
    }


    /*public static double[] AggregateDoubleArray(this IEnumerable<IEnumerable<double>> source)
    {
        return source.Where(x => x is not null).DefaultIfEmpty(Enumerable.Empty<double>()).Aggregate((x, y) => x.ZipLongest(y, (a, b) => a + b)).ToArray();
    }*/


    public static double[] Normalize(this IEnumerable<double> source)
    {
        var norm = source?.Sum() ?? 0d;
        if (Math.Abs(norm) < Consts.Precision)
            return Enumerable.Empty<double>().ToArray();
        return source.Select(v => v / norm).ToArray();
    }


    public static double CheckStringForExponentialAndConvertToDouble(this string s)
    {
        if (s == null) return default;
        if (double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var doubleValue))
            return doubleValue;
        ApplicationMessage.Log(Error.ParsingInvalidOrScientificValue, s);
        return default;
    }


    public static bool Contains<T>(this T value, T lookingForFlag)
        where T : struct
    {
        var intValue = (int)(object)value;
        var intLookingForFlag = (int)(object)lookingForFlag;
        return (intValue & intLookingForFlag) == intLookingForFlag;
    }


    public static bool ContainsEnum<T>(this IEnumerable<T> values, T lookingForFlag) where T : struct
    {
        return values.Any(value => value.Contains(lookingForFlag));
    }

    public static class IdentityReader<T> where T : class
    {
        private static Dictionary<string, Func<T, string>> ExpressionsByExcludedProperties = new();

        public static string Concat(string first, string second)
        {
            return first + " " + second;
        }

        public static string GetString(object item)
        {
            return item == null ? "" : item.ToString();
        }

        private static Func<T, string> GetToIdentityExpression(string[] excludedProperties)
        {
            var pm = Expression.Parameter(typeof(T));
            var expression = typeof(T).GetIdentityProperties().Where(x => !excludedProperties.Contains(x.Name))
                .SelectMany(x => new Expression[]
                    {
                        Expression.Constant(x.Name.ToString() + ":"),
                        Expression.Call(typeof(IdentityReader<T>).GetMethod(nameof(GetString)), Expression.Convert(Expression.Property(pm, x.Name), typeof(object)))
                    }
                ).Aggregate((x, y) => Expression.Call(typeof(IdentityReader<T>).GetMethod(nameof(Concat)), x, y));
            return Expression.Lambda<Func<T, string>>(expression, pm).Compile();
        }

        public static string ToString(T x, string[] excludedProperties)
        {
            var key = string.Join(",", excludedProperties.OrderBy(x => x));
            if (!ExpressionsByExcludedProperties.TryGetValue(key, out var expression))
            {
                ExpressionsByExcludedProperties[key] = GetToIdentityExpression(excludedProperties);
                return ExpressionsByExcludedProperties[key](x);
            }

            return expression(x);
        }
    }

    public static string ToIdentityString<T>(this T variable, string[] ignoreProperties = null) where T : class
    {
        if (ignoreProperties == null) ignoreProperties = new string[0];
        return IdentityReader<T>.ToString(variable, ignoreProperties);
    }

    public static IEnumerable<IfrsVariable> AggregateProjections(this IEnumerable<IfrsVariable> source)
    {
        return source
            .GroupBy(x => new
                {
                    EstimateType = x.EstimateType,
                    AmountType = x.AmountType,
                    EconomicBasis = x.EconomicBasis,
                    AccidentYear = x.AccidentYear,
                    DataNode = x.DataNode,
                    AocType = x.AocType,
                    Novelty = x.Novelty
                },
                x => x.Values,
                (key, values) =>
                    new IfrsVariable() with
                    {
                        Values = values.AggregateDoubleArray(),
                        AmountType = key.AmountType,
                        EstimateType = key.EstimateType,
                        EconomicBasis = key.EconomicBasis,
                        AccidentYear = key.AccidentYear,
                        DataNode = key.DataNode,
                        AocType = key.AocType,
                        Novelty = key.Novelty
                    });
    }

    public static ProjectionConfiguration[] SortRelevantProjections(this ProjectionConfiguration[] pc, int month)
    {
        return pc.Where(x => x.Shift > 0 || x.TimeStep == month || (x.TimeStep > month && x.TimeStep % Consts.MonthInAQuarter == 0))
            .OrderBy(x => x.Shift)
            .ThenBy(x => x.TimeStep)
            .ToArray();
    }
}