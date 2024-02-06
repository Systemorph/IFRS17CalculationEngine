using Microsoft.Extensions.DependencyInjection;
using OpenSmc.Fixture;
using OpenSmc.Messaging;
using OpenSmc.ServiceProvider;
using System.Collections.Immutable;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.Domain.Test;

public class DataHubTestBase : TestBase
{

    protected record Address;

    protected record DataHubEvent : IRequest<object>;

    TestDataPlugin testPlugin;
    class TestDataPlugin : MessageHubPlugin
    {
        public TestDataPlugin(IMessageHub hub) :  base(hub)
        {
            Register(HandleMessage);
        }

        public ImmutableList<object> Events { get; private set; } = ImmutableList<object>.Empty;
        
        public override async Task StartAsync()
        {
            Events = Events.Add("Starting");
            await Task.Delay(1000);
            Events = Events.Add("Started");
        }

        public IMessageDelivery HandleMessage(IMessageDelivery request)
        {
            Events = Events.Add("Handled");
            Hub.Post("Handled", o => o.ResponseFor(request));
            return request.Processed();
        }
    }

    [Inject] protected IMessageHub Hub;

    protected DataHubTestBase(ITestOutputHelper output) : base(output)
    {
        Services.AddSingleton<IMessageHub>(sp => sp.CreateMessageHub(new Address(),
            conf => conf.AddPlugin(hub => testPlugin = new TestDataPlugin(hub))));
    }

    protected virtual MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration) => configuration;

    protected virtual IMessageHub GetHost()
    {
        return Hub.GetHostedHub(new Address(), ConfigureHost);
    }

    public override async Task DisposeAsync()
    {
        await Hub.DisposeAsync();
    }

}