using Microsoft.Extensions.DependencyInjection;
using OpenSmc.Fixture;
using OpenSmc.Messaging;
using OpenSmc.ServiceProvider;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.Domain.Test;

public class DataHubTestBase : TestBase
{
    protected record RouterAddress;

    protected record HostAddress;

    [Inject] protected IMessageHub Router;
    protected DataHubTestBase(ITestOutputHelper output) : base(output)
    {
        Services.AddSingleton<IMessageHub>(sp => sp.CreateMessageHub(new RouterAddress(),
            conf => conf
                .WithRoutes(forward => forward
                    .RouteAddressToHostedHub<HostAddress>(ConfigureHost)
                )));
    }

    protected virtual MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration) => configuration;

    protected virtual IMessageHub GetHost()
    {
        return Router.GetHostedHub(new HostAddress(), ConfigureHost);
    }
    
    public override async Task DisposeAsync()
    {
        // TODO V10: This should dispose the other two. (18.01.2024, Roland Buergi)
        await Router.DisposeAsync();
    }

}