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
EUR,2050,1,1.1,Average,";

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

        //Import of LiabilityType
        var importRequest = new ImportRequest(ExchangeRateData);
        var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new ParameterImportAddress(new HostAddress())));
        importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);

        //Assert changes Count
        exchangeRateData = await client.AwaitResponse(new GetManyRequest<ExchangeRate>(), o => o.WithTarget(new ParameterDataAddress(new HostAddress())));

        //Assert data changed
        exchangeRateData.Message.Items.Should().HaveCount(13);

        exchangeRateData.Message.Items.Where(x => x.Year == 2050).Should().HaveCount(1);
        exchangeRateData.Message.Items.Single(x => x.Year == 2050).FxToGroupCurrency.Should().BeApproximately(1.1, 1E-8);
    }
}


//Test Import of Parameters
//public class ImportParameterDataTest(ITestOutputHelper output) : HubTestBase(output)
//    {
//        private static readonly Dictionary<Type, IEnumerable<object>> FinancialDataDomain = new()
//        {
//            { typeof(YieldCurve), Array.Empty<YieldCurve>() },
//            { typeof(ExchangeRate), Array.Empty<ExchangeRate>() },
//        };

//    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
//    {
//        return base.ConfigureHost(configuration)
//            .AddData(data => data.WithDataSource
//            (
//                nameof(DataSource),
//                source => source.ConfigureCategory(FinancialDataDomain)
//            ))
//            .AddImport(import => import);
//    }

//    [Fact(Skip = "Import suffers from the problem of definition of Key. TODO: apply the same solution as in ReferenceData.")]
//    public async Task ImportParameterTest()
//    {
//        var client = GetClient();
//        var importRequest = new ImportRequest(YieldCurveCsv);
//        var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new HostAddress()));
//        importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);

//        var ycItems = await client.AwaitResponse(new GetManyRequest<YieldCurve>(), o => o.WithTarget(new HostAddress()));
//        var erItems = await client.AwaitResponse(new GetManyRequest<ExchangeRate>(), o => o.WithTarget(new HostAddress()));

//        ycItems.Message.Items.Should().HaveCount(4);
//        erItems.Message.Items.Should().HaveCount(12);
//    }

//    private const string YieldCurveCsv =
//        @"@@YieldCurve
//Year,Month,Currency,Scenario,Name,Values0,Values1,Values2,Values3
//2019,12,CHF,,0,0,0.015,0.02
//2019,12,XTSHY,,0.85,0.85,0.85,0.85
//2019,12,EUR,,0,0,0,0
//2019,12,EUR,NoDiscount,0,0,0,0
//2019,12,EUR,3PCT,0.03,0.03,0.03,0.03
//@@ExchangeRate
//Currency,Year,Month,FxType,FxToGroupCurrency
//EUR,2021,3,Average,1.2012
//EUR,2021,3,Spot,1.2013
//EUR,2020,12,Average,1.2014
//EUR,2020,12,Spot,1.2015
//USD,2021,3,Average,1.2016
//USD,2021,3,Spot,1.2017
//USD,2020,12,Average,1.2018
//USD,2020,12,Spot,1.2019
//GBP,2021,3,Average,1.4016
//GBP,2021,3,Spot,1.4017
//GBP,2020,12,Average,1.4018
//GBP,2020,12,Spot,1.4019";
//}