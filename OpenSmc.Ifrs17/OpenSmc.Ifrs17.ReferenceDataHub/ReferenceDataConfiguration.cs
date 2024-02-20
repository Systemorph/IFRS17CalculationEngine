using AngleSharp.Io;
using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Import;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

public static class DataHubConfiguration
{

    public static MessageHubConfiguration ConfigureReferenceData(this MessageHubConfiguration configuration)
    {
        return configuration
            .AddData(dc => dc
                .WithDataSource("ReferenceDataSource", ds => ds)
                .WithInMemoryInitialization(Initialization(TemplateDimensions.Csv)))
            .AddImport(import => import);
    }

    private static Action<IMessageHub> Initialization(string csvFile)
    {
        return hub => hub.Post(new ImportRequest(csvFile));
    }
}


