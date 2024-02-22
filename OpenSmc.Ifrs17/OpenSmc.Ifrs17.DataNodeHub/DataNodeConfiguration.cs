using OpenSmc.Data;
using OpenSmc.Import;
using OpenSmc.Messaging;
using OpenSmc.Reflection;
using OpenSmc.Ifrs17.ReferenceDataHub;

namespace OpenSmc.Ifrs17.DataNodeHub;

public static class DataHubConfiguration
{
    private static readonly TypeArrayKey Referencetypes = ReferenceDataDomain.GetKey();
    public static MessageHubConfiguration ConfigureDataNodes(this MessageHubConfiguration configuration, Dictionary<Type, IEnumerable<object>> types)
    {
        return configuration
            .AddImport(import => import)
            .AddData(dc => dc
                .WithDataSource("DataNodeSource", ds => ds.ConfigureCategory(types))
                .WithInitialization(InitializationAsync(configuration, TemplateDataNodes.Csv)));
    }

    private static Func<IMessageHub, CancellationToken, Task> InitializationAsync(MessageHubConfiguration config, string csvFile)
    {
        return async (hub, cancellationToken) =>
        {
            var request = new ImportRequest(csvFile);
            await hub.AwaitResponse(request, o => o.WithTarget(new ImportAddress(config.Address)), cancellationToken);
        };
    }
}