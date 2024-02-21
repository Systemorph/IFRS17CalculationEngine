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
                .WithInMemoryInitialization(Initialization(TemplateParameter.Csv)))
            .AddImport(import => import);
    }

    private static Action<IMessageHub> Initialization(string csvFile)
    {
        return hub => hub.Post(new ImportRequest(csvFile));
    }
}