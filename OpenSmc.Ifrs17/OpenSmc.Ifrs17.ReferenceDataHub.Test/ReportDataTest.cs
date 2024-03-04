using FluentAssertions;
using OpenSmc.Data;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.ReportHub;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.Hub.Test;

public class ReportDataTest(ITestOutputHelper output) : HubTestBase(output)
{
    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
    {
        return base.ConfigureHost(configuration).ConfigureReportDataHub();
    }

    [Fact]
    public async Task InitReportDataTest()
    {
        var client = GetClient();

        var reportAddress = new ReportAddress(new HostAddress(), 2020, 12, "CH", "Bla");

        var reportVariable = await client.AwaitResponse(new GetManyRequest<ReportVariable>(), o => o.WithTarget(reportAddress));

        reportVariable.Message.Items.Should().HaveCount(2);
    }
}
