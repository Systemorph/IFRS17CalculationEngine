using OpenSmc.Data;
using OpenSmc.Import;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.IfrsVariableHub;

public static class IfrsVariablesHubConfiguration
{
    //Configuration 1: Use a dictionary to initialize the DataHub 
    public static MessageHubConfiguration ConfigureIfrsVarsDictInit(this MessageHubConfiguration configuration)
    {
        // TODO: WIP
        return configuration
            .AddData(dc => dc.WithDataSource("ParameterDataSource",
                ds => ds));
    }

    //Configuration 2: Use Import of TemplateParameter.CSV to Initialize the DataHub.
    public static MessageHubConfiguration ConfigureIfrsVarsDataImportInit(this MessageHubConfiguration configuration)
    {
        // TODO: WIP
        return configuration
            .AddImport(import => import)
            .AddData(dc => dc
                .WithDataSource("ParameterDataSource",
                    ds => ds)
                .WithInitialization(IfrsVarsInit(configuration)));
    }

    public static Func<IMessageHub, CombinedWorkspaceState, CancellationToken, Task> IfrsVarsInit(MessageHubConfiguration config)
    {
        return async (hub, workspace, cancellationToken) =>
        {
            // TODO: WIP
            await Task.CompletedTask;
        };
    }

}
