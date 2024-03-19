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

        var exchangeRateData = await client.AwaitResponse(new GetManyRequest<ExchangeRate>(), o => o.WithTarget(new HostAddress()));
        var cdrData = await client.AwaitResponse(new GetManyRequest<CreditDefaultRate>(),o => o.WithTarget(new HostAddress()));
        var partnerRatingData = await client.AwaitResponse(new GetManyRequest<PartnerRating>(), o => o.WithTarget(new HostAddress()));

        exchangeRateData.Message.Items.Should().HaveCount(12);
        cdrData.Message.Items.Should().HaveCount(22);
        partnerRatingData.Message.Items.Should().HaveCount(3);
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

        //Get ActualCountPerType
        var exchangeRateData = await client.AwaitResponse(new GetManyRequest<ExchangeRate>(), o => o.WithTarget(new ParameterDataAddress(new HostAddress())));
        var cdrData = await client.AwaitResponse(new GetManyRequest<CreditDefaultRate>(), o => o.WithTarget(new ParameterDataAddress(new HostAddress())));
        var partnerRatingData = await client.AwaitResponse(new GetManyRequest<PartnerRating>(), o => o.WithTarget(new ParameterDataAddress(new HostAddress())));

        //Assert Count per Type
        exchangeRateData.Message.Items.Should().HaveCount(12);
        cdrData.Message.Items.Should().HaveCount(22);
        partnerRatingData.Message.Items.Should().HaveCount(3);

        //Import exchange rate
        var importRequest = new ImportRequest(ExchangeRateData);
        var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new ParameterImportAddress(new HostAddress())));
        importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);

        //Get data from DataHub after Import is completed (delay)
        await Task.Delay(300);
        exchangeRateData = await client.AwaitResponse(new GetManyRequest<ExchangeRate>(), o => o.WithTarget(new ParameterDataAddress(new HostAddress())));

        //Assert data changed
        exchangeRateData.Message.Items.Should().HaveCount(13);
        exchangeRateData.Message.Items.Where(x => x.Year == 2050).Should().HaveCount(1);
        exchangeRateData.Message.Items.Single(x => x.Year == 2050).FxToGroupCurrency.Should().BeApproximately(1.1, 1E-8);
    }
}