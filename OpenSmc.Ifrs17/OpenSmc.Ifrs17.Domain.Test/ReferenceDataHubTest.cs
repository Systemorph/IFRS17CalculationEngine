using FluentAssertions;
using OpenSmc.Data;
using OpenSmc.Domain.Abstractions;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;


namespace OpenSmc.Ifrs17.Domain.Test;
public class ReferenceDataHubTest(ITestOutputHelper output) : HubTestBase(output)
{

    private readonly ReferenceData _referenceData = new();

    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
        => base.ConfigureHost(configuration).ConfigurationReferenceDataHub();

    [Fact]
    public async Task InitializationReferenceDataHubAoc()
    {
        var client = GetClient();
        var response = await client.AwaitResponse(new GetManyRequest<AocStep>(), o => o.WithTarget(new HostAddress()));
        var expected = new GetManyResponse<AocStep>(_referenceData.ReferenceAocSteps.Length, 
            _referenceData.ReferenceAocSteps);
        response.Message.Should().BeAssignableTo<GetManyResponse<AocStep>>();
        response.Message.Total.Should().Be(expected.Total);
        foreach (var element in expected.Items.Select(x => (x.AocType, x.Novelty)))
            response.Message.Items.Select(x => (x.AocType, x.Novelty)).Should().Contain(element);
    }


    [Fact]
    public async Task InitializationReferenceDataHubAmountType()
    {
        var client = GetClient();
        var response = await client.AwaitResponse(new GetManyRequest<AmountType>(),
            o => o.WithTarget(new HostAddress()));
        var expected = new GetManyResponse<AmountType>(_referenceData.ReferenceAmountTypes.Length,
            _referenceData.ReferenceAmountTypes);
        response.Message.Should().BeAssignableTo<GetManyResponse<AmountType>>();
        response.Message.Total.Should().Be(expected.Total);
        foreach (var element in expected.Items.Select(x => new Dimension(){SystemName = x.SystemName, DisplayName = x.DisplayName}))
        {
            response.Message.Items
                .Select(x => new Dimension(){SystemName = x.SystemName, DisplayName = x.DisplayName})
                .Should().Contain(element);
        }

        response.Message.Should().NotBeEquivalentTo(expected);
    }

    /*[Fact]
    public async Task HelloWorldFromClient()
    {
        var client = GetClient();
        var response = await client.AwaitResponse(new WakeUpEvent(), o => o.WithTarget(new HostAddress()));
        response.Should().BeAssignableTo<IMessageDelivery<WakeUpRequest>>();
    }

    [Fact]
    public async Task ClientToServerWithMessageTraffic()
    {
        var client = GetClient();
        var clientOut = client.AddObservable();
        var messageTask = clientOut.Where(d => d.Message is HelloEvent).ToArray().GetAwaiter();
        var overallMessageTask = clientOut.ToArray().GetAwaiter();

        var response = await client.AwaitResponse(new WakeUpEvent(), o => o.WithTarget(new HostAddress()));
        response.Should().BeAssignableTo<IMessageDelivery<WakeUpRequest>>();

        await DisposeAsync();

        var helloEvents = await messageTask;
        var overallMessages = await overallMessageTask;
        using (new AssertionScope())
        {
            helloEvents.Should().ContainSingle();
            overallMessages.Should().HaveCountLessThan(10);
        }
    }

    [Fact]
    public async Task Subscribers()
    {
        // arrange: initiate subscription from client to host
        var client = GetClient();
        await client.AwaitResponse(new WakeUpEvent(), o => o.WithTarget(new HostAddress()));
        var clientOut = client.AddObservable().Timeout(500.Milliseconds());
        var clientMessagesTask = clientOut.Select(d => d.Message).OfType<WakeUpRequest>().FirstAsync().GetAwaiter();

        // act 
        var host = GetHost();
        host.Post(new WakeUpRequest(), o => o.WithTarget(MessageTargets.Subscribers));

        // assert
        var clientMessages = await clientMessagesTask;
        clientMessages.Should().BeAssignableTo<WakeUpRequest>();
    }*/

}
