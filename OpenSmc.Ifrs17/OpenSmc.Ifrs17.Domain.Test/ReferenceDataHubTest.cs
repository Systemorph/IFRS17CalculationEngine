﻿using FluentAssertions;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Messaging;
using Xunit.Abstractions;


namespace OpenSmc.Ifrs17.Domain.Test;
public class ReferenceDataHubTest : DataHubTestBase
{

    record WakeUpRequest : IRequest<Currency>;
    record Currency;


    public ReferenceDataHubTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
        => DataHubConfiguration.ConfigurationReferenceDataHub(configuration);


    [Fact]
    public async Task InitilizationReferenceDataHub()
    {
        var host = GetHost();
        var response = await host.AwaitResponse<Currency>(new WakeUpRequest(), o => o.WithTarget(new Address()));
        response.Should().BeAssignableTo<IMessageDelivery<Currency>>();
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