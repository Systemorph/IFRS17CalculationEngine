using FluentAssertions;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.ReportHub;
using OpenSmc.Messaging;
using OpenSmc.Reporting;
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

        var reportRequest = new ReportRequest();

        var reportResponse = await client.AwaitResponse(reportRequest, o => o.WithTarget(reportAddress));

        var gridOptions = reportResponse.Message.GridOptions;
        gridOptions.Should().NotBeNull();
    }
}
