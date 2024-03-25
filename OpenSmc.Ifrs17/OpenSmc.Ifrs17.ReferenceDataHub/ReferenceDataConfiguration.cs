using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.Utils;
using OpenSmc.Import;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

public static class ReferenceDataHubConfiguration
{
    public static MessageHubConfiguration ConfigureReferenceDataDictInit(this MessageHubConfiguration configuration)
    {
        return configuration
            .AddData(dc => dc
                .FromConfigurableDataSource("ReferenceData", ds => ds
                    .ConfigureCategory(TemplateData.TemplateReferenceData)
                    .WithType<AocConfiguration>(t => t
                        .WithKey(x => (x.Year, x.Month, x.AocType, x.Novelty))
                        .WithInitialData(TemplateData.AocConfiguration[typeof(AocConfiguration)]))
                    .WithType<PartitionByReportingNode>(t => t
                        .WithKey(x => x.ReportingNode)
                        .WithInitialData((IEnumerable<PartitionByReportingNode>)TemplateData.Partitions[typeof(PartitionByReportingNode)]))
                    .WithType<PartitionByReportingNodeAndPeriod>(t => t
                        .WithKey(x => (x.ReportingNode, x.Year, x.Month, x.Scenario))
                        .WithInitialData((IEnumerable<PartitionByReportingNodeAndPeriod>)TemplateData.Partitions[typeof(PartitionByReportingNodeAndPeriod)]))
                ));
    }

    //Configuration DataHub Golden Copy. 
    public static MessageHubConfiguration ConfigureReferenceDataModelHub(this MessageHubConfiguration configuration)
    {
        var address = new ReferenceDataAddress(configuration.Address);
        return configuration
            .WithHostedHub(address, config => config
                .ConfigureReferenceDataDictInit()
        );
    }

    //Configuration of ReferenceData Import Hub
    public static MessageHubConfiguration ConfigureReferenceDataImportHub(this MessageHubConfiguration configuration)
    {
        var refDataAddress = new ReferenceDataAddress(configuration.Address);
        var refDataImportAddress = new ReferenceDataImportAddress(configuration.Address);

        return configuration
            .WithHostedHub(refDataImportAddress, config => config
                .AddImport(data => data
                    .FromHub(refDataAddress, ds => ds
                        .ConfigureTypesFromCategory(TemplateData.TemplateReferenceData)
                        .WithType<AocConfiguration>()
                        .WithType<PartitionByReportingNode>()
                        .WithType<PartitionByReportingNodeAndPeriod>()
                        ),
                    import => import
                )
            );
    }
}