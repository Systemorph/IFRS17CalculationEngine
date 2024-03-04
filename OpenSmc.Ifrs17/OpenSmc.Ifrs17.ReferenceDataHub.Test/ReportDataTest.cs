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
    public async Task InitParameterDataTest()
    {
        var client = GetClient();

        var exchangeRateData = await client.AwaitResponse(new GetManyRequest<ExchangeRate>(), o => o.WithTarget(new HostAddress()));
        var cdrData = await client.AwaitResponse(new GetManyRequest<CreditDefaultRate>(), o => o.WithTarget(new HostAddress()));
        var partnerRatingData = await client.AwaitResponse(new GetManyRequest<PartnerRating>(), o => o.WithTarget(new HostAddress()));

        exchangeRateData.Message.Items.Should().HaveCount(12);
        cdrData.Message.Items.Should().HaveCount(22);
        partnerRatingData.Message.Items.Should().HaveCount(3);
    }
}
