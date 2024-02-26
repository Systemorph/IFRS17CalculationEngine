using OpenSmc.Data;
using OpenSmc.Ifrs17.DataNodeHub;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Messaging;
using OpenSmc.Import;

namespace OpenSmc.Ifrs17.ReportHub;

public static class ReportHubConfiguration
{
    public static MessageHubConfiguration ConfigureReportHubSimple(this MessageHubConfiguration configuration)
    {
        var refDataAddress = new ReferenceDataAddress(configuration.Address);
        var dataNodeAddress = new DataNodeAddress(configuration.Address);
        var reportAddress = new ReportAddress(configuration.Address);

        return configuration
            .WithHostedHub(refDataAddress, config => config
                .AddImport(import => import)
                .AddData(dc => dc
                .WithDataSource("ReferenceDataSource",
                    ds => ds.ConfigureCategory(ReferenceDataHubConfiguration.ReferenceDataDomain)
                            .ConfigureCategory(ReferenceDataHubConfiguration.ReferenceDataDomainExtra))
                .WithInitialization(ReferenceDataHubConfiguration.RefDataInit(configuration, TemplateDimensions.Csv))))

            .WithHostedHub(dataNodeAddress, config => config
                .AddImport(import => import)
                .AddData(dc => dc
                .WithDataSource("DataNodeDataSource",
                    ds => ds.ConfigureCategory(DataNodeHubConfiguration.DataNodeDomain).ConfigureCategory(DataNodeHubConfiguration.DataNodeDomainExtra))
                .WithInitialization(DataNodeHubConfiguration.DataNodeInit(configuration, TemplateDimensions.Csv, refDataAddress))))

            .WithRoutes(routes => routes
                .RouteMessage<GetManyRequest>(_ => refDataAddress));
    }
}
