using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Import;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ParameterDataHub;

public static class ParameterConfiguration
{
    //Configuration 1: Use a dictionary to initialize the DataHub 
    public static MessageHubConfiguration ConfigureParameterDataDictInit(this MessageHubConfiguration configuration)
    {
        return configuration
            .AddData(dc => dc.WithDataSource("ParameterDataSource",
                    ds => ds.WithType<ExchangeRate>(t => t.WithKey(x => (x.Year, x.Month, x.Scenario, x.Currency)).WithInitialData(TemplateData.ExchangeRateData)
                )));

    }

    //Configuration 2: Use Import of TemplateParameter.CSV to Initialize the DataHub.
    //This does not currently work due to Key attribute being on the Id prop not instantiated during Initialization.
    public static MessageHubConfiguration ConfigureParameterDataImportInit(this MessageHubConfiguration configuration)
    {
        return configuration
            .AddData(dc => dc
                .WithDataSource("ParameterDataSource", ds => ds)
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