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

        //Get Count
        var ifrsVariables = await client.AwaitResponse(new GetManyRequest<IfrsVariable>(), o => o.WithTarget(new HostAddress()));

        //Assert Count
        ifrsVariables.Message.Items.Should().HaveCount(40);
    }
}