using FluentAssertions;
using OpenSmc.Data;
using OpenSmc.Hub.Fixture;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.DataTypes.Constants;

namespace OpenSmc.Ifrs17.ReferenceDataHub.Test;

public class ReferenceDataDictInitTest(ITestOutputHelper output) : HubTestBase(output)
{
    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
    {
        return base.ConfigureHost(configuration).ConfigureReferenceDataDictInit();
    }

    [Fact]
    public async Task InitializedDataTest()
    {
        var client = GetClient();

        var atItems = await client.AwaitResponse(new GetManyRequest<AmountType>(), o => o.WithTarget(new HostAddress()));
        var datItems = await client.AwaitResponse(new GetManyRequest<DeferrableAmountType>(), o => o.WithTarget(new HostAddress()));
        var aocItems = await client.AwaitResponse(new GetManyRequest<AocType>(), o => o.WithTarget(new HostAddress()));
        var aoccItems = await client.AwaitResponse(new GetManyRequest<AocConfiguration>(), o => o.WithTarget(new HostAddress()));
        var stItems = await client.AwaitResponse(new GetManyRequest<StructureType>(), o => o.WithTarget(new HostAddress()));
        var crrItems = await client.AwaitResponse(new GetManyRequest<CreditRiskRating>(), o => o.WithTarget(new HostAddress()));
        var cItems = await client.AwaitResponse(new GetManyRequest<Currency>(), o => o.WithTarget(new HostAddress()));
        var ecItems = await client.AwaitResponse(new GetManyRequest<EconomicBasis>(), o => o.WithTarget(new HostAddress()));
        var esItems = await client.AwaitResponse(new GetManyRequest<EstimateType>(), o => o.WithTarget(new HostAddress()));
        var ltItems = await client.AwaitResponse(new GetManyRequest<LiabilityType>(), o => o.WithTarget(new HostAddress()));
        var lobItems = await client.AwaitResponse(new GetManyRequest<LineOfBusiness>(), o => o.WithTarget(new HostAddress()));
        var nItems = await client.AwaitResponse(new GetManyRequest<Novelty>(), o => o.WithTarget(new HostAddress()));
        var otItems = await client.AwaitResponse(new GetManyRequest<OciType>(), o => o.WithTarget(new HostAddress()));
        var pItemsn = await client.AwaitResponse(new GetManyRequest<Partner>(), o => o.WithTarget(new HostAddress()));
        var bvtItems = await client.AwaitResponse(new GetManyRequest<BsVariableType>(), o => o.WithTarget(new HostAddress()));
        var pvt = await client.AwaitResponse(new GetManyRequest<PnlVariableType>(), o => o.WithTarget(new HostAddress()));
        var rdItems = await client.AwaitResponse(new GetManyRequest<RiskDriver>(), o => o.WithTarget(new HostAddress()));
        var sItems = await client.AwaitResponse(new GetManyRequest<Scenario>(), o => o.WithTarget(new HostAddress()));
        var vaItems = await client.AwaitResponse(new GetManyRequest<ValuationApproach>(), o => o.WithTarget(new HostAddress()));
        var pcItems = await client.AwaitResponse(new GetManyRequest<ProjectionConfiguration>(), o => o.WithTarget(new HostAddress()));

        atItems.Message.Items.Should().HaveCount(17);
        datItems.Message.Items.Should().HaveCount(2);
        aocItems.Message.Items.Should().HaveCount(17);
        aoccItems.Message.Items.Should().HaveCount(21);
        stItems.Message.Items.Should().HaveCount(8);
        crrItems.Message.Items.Should().HaveCount(22);
        cItems.Message.Items.Should().HaveCount(10);
        ecItems.Message.Items.Should().HaveCount(3);
        esItems.Message.Items.Should().HaveCount(15);
        ltItems.Message.Items.Should().HaveCount(2);
        lobItems.Message.Items.Should().HaveCount(15);
        nItems.Message.Items.Should().HaveCount(3);
        otItems.Message.Items.Should().HaveCount(1);
        pItemsn.Message.Items.Should().HaveCount(2);
        bvtItems.Message.Items.Should().HaveCount(1);
        pvt.Message.Items.Should().HaveCount(50);
        rdItems.Message.Items.Should().HaveCount(1);
        sItems.Message.Items.Should().HaveCount(16);
        vaItems.Message.Items.Should().HaveCount(2);
        pcItems.Message.Items.Should().HaveCount(20);
    }
}