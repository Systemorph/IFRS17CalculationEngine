using OpenSmc.Data;

namespace OpenSmc.Ifrs17.Utils
{
    public static class ExtensionMethods
    {
        public static TDataSource ConfigureCategory<TDataSource>(this TDataSource dataSource, IDictionary<Type, IEnumerable<object>> typeAndInstance)
            where TDataSource : DataSource<TDataSource>
            => typeAndInstance.Aggregate(dataSource,
                (ds, kvp) =>
                    ds.WithType(kvp.Key, t => t.WithInitialData(kvp.Value)));
    }
}
