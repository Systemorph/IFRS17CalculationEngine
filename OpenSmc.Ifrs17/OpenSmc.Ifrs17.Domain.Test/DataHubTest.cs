using System.Reactive.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
using OpenSmc.DataSource.Abstractions;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;
using OpenSmc.Ifrs17.Re;

namespace OpenSmc.Ifrs17.Domain.Test;
public class DataHubTest : HubTestBase
{

    record WakeUpRequest : MediatR.IRequest<WakeUptEvent>;
    record WakeUptEvent;


    public DataHubTest(ITestOutputHelper output) : base(output)
    {
    }
    public static MessageHubConfiguration ConfigurationReferenceDataHub(this MessageHubConfiguration financialDataConfiguration)
    {
        // TODO: this needs to be registered in the higher level
        var dataSource = financialDataConfiguration.ServiceProvider.GetService<IDataSource>();

        return financialDataConfiguration
            .AddData(data => data
                    .WithDimension<LineOfBusiness>(dataSource)
                    .WithDimension<Currency>(dataSource)
                /*.WithDimension<Scenario>(dataSource)
                .WithDimension<AmountType>(dataSource)
                .WithDimension<OciType>(dataSource)
                .WithDimension<AocType>(dataSource)
                .WithDimension<CreditRiskRating>(dataSource)
                .WithDimension<Currency>(dataSource)
                .WithDimension<DeferrableAmountType>(dataSource)*/
            );
    }
    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
        => configuration
            .WithHandler<WakeUptEvent>((hub, request) =>
            {
                hub.Post(new WakeUpRequest(), options => options.ResponseFor(request));
                return request.Processed();
            });


    [Fact]
    public async Task HelloWorld()
    {
        var host = GetHost();
        var response = await host.AwaitResponse(new WakeUptEvent(), o => o.WithTarget(new HostAddress()));
        response.Should().BeAssignableTo<IMessageDelivery<WakeUpRequest>>();
    }


    [Fact]
    public async Task HelloWorldFromClient()
    {
        var client = GetClient();
        var response = await client.AwaitResponse(new WakeUptEvent(), o => o.WithTarget(new HostAddress()));
        response.Should().BeAssignableTo<IMessageDelivery<WakeUpRequest>>();
    }

    [Fact]
    public async Task ClientToServerWithMessageTraffic()
    {
        var client = GetClient();
        var clientOut = client.AddObservable();
        var messageTask = clientOut.Where(d => d.Message is HelloEvent).ToArray().GetAwaiter();
        var overallMessageTask = clientOut.ToArray().GetAwaiter();

        var response = await client.AwaitResponse(new WakeUptEvent(), o => o.WithTarget(new HostAddress()));
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
        await client.AwaitResponse(new WakeUptEvent(), o => o.WithTarget(new HostAddress()));
        var clientOut = client.AddObservable().Timeout(500.Milliseconds());
        var clientMessagesTask = clientOut.Select(d => d.Message).OfType<WakeUpRequest>().FirstAsync().GetAwaiter();

        // act 
        var host = GetHost();
        host.Post(new WakeUpRequest(), o => o.WithTarget(MessageTargets.Subscribers));

        // assert
        var clientMessages = await clientMessagesTask;
        clientMessages.Should().BeAssignableTo<WakeUpRequest>();
    }

}
