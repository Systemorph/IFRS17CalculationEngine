using System.Reactive.Linq;
using FluentAssertions;
using OpenSmc.Activities;
using OpenSmc.Data;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.ParameterDataHub;
using OpenSmc.Import;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.Hub.Test;

//Test ParameterDataDictInit configuration
public class ParameterDataDictInitTest(ITestOutputHelper output) : HubTestBase(output)
{
    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
    {
        return base.ConfigureHost(configuration)
            .ConfigureParameterDataDictInit();
    }

    [Fact]
    public async Task InitParameterDataTest()
    {
        var client = GetClient();
        var workspace = client.GetWorkspace(); // TODO V10: client should be configured to become a member of DataSync flow (2024/03/21, Dmitry Kalabin)

        var exchangeRateData = await workspace.GetObservable<ExchangeRate>().FirstAsync();
        var cdrData = await workspace.GetObservable<CreditDefaultRate>().FirstAsync();
        var partnerRatingData = await workspace.GetObservable<PartnerRating>().FirstAsync();

        exchangeRateData.Should().HaveCount(12);
        cdrData.Should().HaveCount(22);
        partnerRatingData.Should().HaveCount(3);
    }
}

//Test ParameterDataImport configuration
public class ParameterImportTest(ITestOutputHelper output) : HubTestBase(output)
{
    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
    {
        return base.ConfigureHost(configuration)
            .ConfigureParameterDataModelHub()
            .ConfigureParameterDataImportHub();
    }

    private const string ExchangeRateData = @"@@ExchangeRate
Currency,Year,Month,FxToGroupCurrency,FxType,Scenario
EUR,2050,1,1.1,Average,,";

    [Fact]
    public async Task ImportTest()
    {
        var client = GetClient();
        var workspace = client.GetWorkspace(); // TODO V10: client should be configured to become a member of DataSync flow (2024/03/21, Dmitry Kalabin)

        //Get ActualCountPerType
        var exchangeRateData = await workspace.GetObservable<ExchangeRate>().FirstAsync();
        var cdrData = await workspace.GetObservable<CreditDefaultRate>().FirstAsync();
        var partnerRatingData = await workspace.GetObservable<PartnerRating>().FirstAsync();

        //Assert Count per Type
        exchangeRateData.Should().HaveCount(12);
        cdrData.Should().HaveCount(22);
        partnerRatingData.Should().HaveCount(3);

        //Import exchange rate
        var importRequest = new ImportRequest(ExchangeRateData);
        var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new ParameterImportAddress(new HostAddress())));
        importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);

        //Get data from DataHub after Import is completed (delay)
        await Task.Delay(300);
        exchangeRateData = await workspace.GetObservable<ExchangeRate>().FirstAsync();

        //Assert data changed
        exchangeRateData.Should().HaveCount(13);
        exchangeRateData.Where(x => x.Year == 2050).Should().HaveCount(1);
        exchangeRateData.Single(x => x.Year == 2050).FxToGroupCurrency.Should().BeApproximately(1.1, 1E-8);
    }
}