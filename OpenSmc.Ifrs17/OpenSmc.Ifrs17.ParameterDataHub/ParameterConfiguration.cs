using OpenSmc.Data;
using OpenSmc.Import;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ParameterDataHub;

public static class ParameterConfiguration
{
    public static MessageHubConfiguration ConfigureReferenceData(this MessageHubConfiguration configuration)
    {
        return configuration
            .AddData(dc => dc
                .WithDataSource("ReferenceDataSource", ds => ds)
                .WithInitialization(InitializationAsync(TemplateParameter.Csv)))
            .AddImport(import => import);
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