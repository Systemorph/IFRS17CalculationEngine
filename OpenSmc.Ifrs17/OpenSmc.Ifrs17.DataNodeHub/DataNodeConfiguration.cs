using OpenSmc.Data;
using OpenSmc.Ifrs17.Utils;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.DataNodeHub;

public static class DataNodeHubConfiguration
{
    public static MessageHubConfiguration ConfigureDataNodeDataDictInit(this MessageHubConfiguration configuration)
    {
        return configuration
            .AddData(dc => dc
                .FromConfigurableDataSource("DataNodeDataSource", ds => ds
                    .ConfigureCategory(TemplateData.DataNodeData)
                ));
    }
}
