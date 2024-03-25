using System.Reactive.Linq;
using FluentAssertions;
using OpenSmc.Data;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;
using OpenSmc.Ifrs17.IfrsVariableHub;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.Hub.Test;

public class IfrsVariableDataDictInitTest(ITestOutputHelper output) : HubTestBase(output)
{
    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
    {
        return base.ConfigureHost(configuration)
            .ConfigureIfrsDataDictInit(TemplateData.year,TemplateData.month,TemplateData.reportinNode,null);
    }

    [Fact]
    public async Task InitIfrsVariableDataTest()
    {
        var client = GetClient();
        var workspace = client.GetWorkspace(); // TODO V10: client should be configured to become a member of DataSync flow (2024/03/21, Dmitry Kalabin)

        //Get Count
        var ifrsVariables = await workspace.GetObservable<IfrsVariable>().FirstAsync();

        //Assert Count
        ifrsVariables.Should().HaveCount(40);
    }
}