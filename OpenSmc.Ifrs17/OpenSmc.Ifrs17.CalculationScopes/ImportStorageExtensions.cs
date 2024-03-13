using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.Constants.Validations;
using static MoreLinq.Extensions.ZipLongestExtension;

namespace OpenSmc.Ifrs17.CalculationScopes;

public static class ImportStorageExtensions
{
    public static T? GetValidElement<T>(this ICollection<T> collection, int index)
    {
        var count = collection.Count;
        if (collection == null || count == 0)
            return default;

        if (index < 0)
        {
            throw new Exception(Error.NegativeIndex.GetMessage());
        }

        return index < count
            ? collection.ElementAt(index)
            : collection.ElementAt(count - 1);
    }

    public static double[] AggregateDoubleArray(this IEnumerable<IEnumerable<double>> source)
    {
        return source.Where(x => x is not null)
            .DefaultIfEmpty(Enumerable.Empty<double>())
            .Aggregate((x, y) => x.ZipLongest(y, (a, b) => a + b)).ToArray();
    }

    public static double[] Normalize(this IEnumerable<double> source)
    {
        var norm = source?.Sum() ?? 0d;
        if (Math.Abs(norm) < Consts.Precision)
            return Enumerable.Empty<double>().ToArray();
        return source.Select(v => v / norm).ToArray();
    }

}
