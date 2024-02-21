using OpenSmc.Data;
using OpenSmc.Import;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

public static class DataHubConfiguration
{

    public static MessageHubConfiguration ConfigureReferenceData(this MessageHubConfiguration configuration, Dictionary<Type, IEnumerable<object>> types)
    {
        return configuration
            .AddImport(import => import)
            .AddData(dc => dc
                .WithDataSource("ReferenceDataSource", ds => ds.ConfigureCategory(types))
                .WithInMemoryInitialization(InitializationAsync(TemplateDimensions.Csv))
                );
    }

    private static Func<IMessageHub, CancellationToken, Task> InitializationAsync(string csvFile)
    {
        return async (hub, cancellationToken) =>
        {
            var request = new ImportRequest(csvFile);
            hub.Post(request);
            await hub.AwaitResponse(request, cancellationToken);
        };
    }
}