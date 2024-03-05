using FluentAssertions;
using OpenSmc.Activities;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Import;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.Hub.Test;

public class ReferenceDataIfrsHubDictInitTest(ITestOutputHelper output) : ReferenceDataIfrsHubTestBase(output)
{
    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
    {
        return base.ConfigureHost(configuration)
                   .ConfigureReferenceDataDictInit();
    }

    [Fact]
    public async Task InitReferenceDataTest()
    {
        var client = GetClient();

        //Get ActualCountPerType
        var actualCountsPerType = await GetActualCountsPerType(client, ExpectedCountPerType.Keys, new HostAddress());

        //Assert Count per Type
        actualCountsPerType.Should().Equal(ExpectedCountPerType);
    }
}


public class ReferenceDataImportTest(ITestOutputHelper output) : ReferenceDataIfrsHubTestBase(output)
{
    private const string LiabilityTypeData = @"@@LiabilityType
SystemName,DisplayName
NewSystemName,NewDisplayName
";
    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
    {
        return base.ConfigureHost(configuration)
            .WithHostedHub(
                new ReferenceDataAddress(configuration.Address),
                config => config
                    .ConfigureReferenceDataDictInit()
            )
            .ConfigureReferenceDataImportHub();
    }

    [Fact (Skip = "Import Hub configuration not working yet.")]
    public async Task ImportTest()
    {
        var client = GetClient();

        //Get ActualCountPerType
        var actualCountsPerType = await GetActualCountsPerType(client, ExpectedCountPerType.Keys, new ReferenceDataAddress(new HostAddress()));

        //Assert Count per Type
        actualCountsPerType.Should().Equal(ExpectedCountPerType);

        //Import of LiabilityType
        var importRequest = new ImportRequest(LiabilityTypeData);
        var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new ReferenceDataImportAddress(new HostAddress())));
        importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);

        //Assert changes
        var newExpectedCountPerType = ExpectedCountPerType.ToDictionary();
        newExpectedCountPerType[typeof(LiabilityType)] += 1;

        actualCountsPerType = await GetActualCountsPerType(client, ExpectedCountPerType.Keys, new ReferenceDataAddress(new HostAddress()));
        actualCountsPerType.Should().Equal(newExpectedCountPerType);
    }
}


//public class ReferenceDataIfrsHubImportInitTest(ITestOutputHelper output) : ReferenceDataIfrsHubTestBase(output)
//{

//    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
//    {
//        return base.ConfigureHost(configuration)
//                   .ConfigureReferenceDataImportInit();
//    }

//    [Fact]
//    public async Task InitReferenceDataTest()
//    {
//        var client = GetClient();

//        //Get ActualCountPerType
//        var actualCountsPerType = await GetActualCountsPerType(client, ExpectedCountPerType.Keys);

//        //Assert Count per Type
//        actualCountsPerType.Should().Equal(ExpectedCountPerType);
//    }
//}


//public class ReferenceDataIfrsHubImportTest(ITestOutputHelper output) : ReferenceDataIfrsHubTestBase(output)
//{
//    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
//    {
//        return base.ConfigureHost(configuration)
//            .AddImport(import => import)
//            .AddData(dc => dc
//                .WithDataSource("ReferenceDataSource",
//                    ds => ds.ConfigureCategory(TemplateData.TemplateReferenceData)
//                            .ConfigureCategory(ReferenceDataHubConfiguration.ReferenceDataDomainExtra)));
//    }

//    [Fact]
//    public async Task ImportDataTest()
//    {
//        // arrange
//        var client = GetClient();
//        var importRequest = new ImportRequest(TemplateDimensions.Csv);

//        // act
//        var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new HostAddress()));

//        //assert Response
//        importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);

//        //Get ActualCountPerType
//        var actualCountsPerType = await GetActualCountsPerType(client, ExpectedCountPerType.Keys);

//        //Assert Count per Type
//        actualCountsPerType.Should().Equal(ExpectedCountPerType);
//    }


//}

public class ReferenceDataIfrsHubTestBase(ITestOutputHelper output) : IfrsHubTestBase(output)
{

    internal static readonly Dictionary<Type, int> ExpectedCountPerType = new()
    {
        { typeof(AmountType), 17 },
        { typeof(DeferrableAmountType), 2 },
        { typeof(AocType), 17 },
        { typeof(AocConfiguration), 21 },
        { typeof(StructureType), 8 },
        { typeof(CreditRiskRating), 22 },
        { typeof(Currency), 10 },
        { typeof(EconomicBasis), 3 },
        { typeof(EstimateType), 15 },
        { typeof(LiabilityType), 2 },
        { typeof(LineOfBusiness), 15 },
        { typeof(Novelty), 3 },
        { typeof(OciType), 1 },
        { typeof(Partner), 2 },
        { typeof(BsVariableType), 1 },
        { typeof(PnlVariableType), 50 },
        { typeof(RiskDriver), 1 },
        { typeof(Scenario), 16 },
        { typeof(ValuationApproach), 2 },
        { typeof(ProjectionConfiguration), 20 },
    };
}
