using OpenSmc.Data;
using OpenSmc.Ifrs17.DataNodeHub;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Messaging;
using OpenSmc.Import;
using OpenSmc.Ifrs17.ParameterDataHub;

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
                .WithDataSource("ReportDataSource",
                    ds => ds.ConfigureCategory(ReferenceDataHubConfiguration.ReferenceDataDomain)
                            .ConfigureCategory(ReferenceDataHubConfiguration.ReferenceDataDomainExtra)
                            .ConfigureCategory(ParameterHubConfiguration.ParametersDomain)
                            .ConfigureCategory(ParameterHubConfiguration.ParametersDomainExtra)
                            .ConfigureCategory(DataNodeHubConfiguration.DataNodeDomain)
                            .ConfigureCategory(DataNodeHubConfiguration.DataNodeDomainExtra))
                .WithInitialization(ReportInit(config))));
    }

    public static Func<IMessageHub, CombinedWorkspaceState, CancellationToken, Task> ReportInit(MessageHubConfiguration config)
    {
        return async (hub, workspace, cancellationToken) =>
        {
            await ReferenceDataHubConfiguration.RefDataInit(config, TemplateDimensions.Csv).Invoke(hub, workspace, cancellationToken);
            await ParameterHubConfiguration.ParametersInit(config, TemplateDimensions.Csv).Invoke(hub, workspace, cancellationToken);
            await DataNodeHubConfiguration.DataNodeInit(config, TemplateDimensions.Csv, null).Invoke(hub, workspace, cancellationToken);

        };
    }
}
