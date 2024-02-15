using OpenSmc.Hub.Fixture;
using FluentAssertions;
using OpenSmc.Activities;
using OpenSmc.Messaging;
using OpenSmc.Data;
using OpenSmc.Import;
using Xunit;
using Xunit.Abstractions;
using OpenSmc.Ifrs17.DataTypes.DataModel;

namespace OpenSmc.Ifrs17.ReferenceDataHub.Test;

public class ImportParameterTest(ITestOutputHelper output) : HubTestBase(output)
{
    private static readonly Dictionary<Type, IEnumerable<object>> FinancialDataDomain
        =
        new() { { typeof(YieldCurve), new[] { new YieldCurve("USD", 2019, 12, null, null, new []{0.0, 0.1, 0.2, 0.0} ) } } };

    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration) =>
       base.ConfigureHost(configuration).AddData(data => data.WithDataSource(nameof(DataSource),
                source => source.WithType<YieldCurve>( yc => yc
                    .WithKey(x => (x.Year, x.Month, x.Currency, x.Scenario, x.Name))
                    .WithInitialData(FinancialDataDomain[typeof(YieldCurve)].Cast<YieldCurve>()))))
            .AddImport(import => import);

    [Fact]
    public async Task ImportFinancialParameterTest()
    {
        var client = GetClient();
        var importRequest = new ImportRequest(YieldCurveCsv);
        var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new HostAddress()));
        importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);

        var items = await client.AwaitResponse(new GetManyRequest<YieldCurve>(),
            o => o.WithTarget(new HostAddress()));
        items.Message.Items.Should().HaveCount(5);
    }

    private const string YieldCurveCsv =
        @"@@YieldCurve,,,,,
Year,Month,Currency,Scenario,Name,Values0,Values1,Values2,Values3
2019,12,CHF,,,0,0,0.015,0.02
2019,12,XTSHY,,,0.85,0.85,0.85,0.85
2019,12,EUR,,,0,0,0,0
2019,12,EUR,,NoDiscount,0,0,0,0
2019,12,EUR,,3PCT,0.03,0.03,0.03,0.03";
}