using System.Reactive.Linq;
using FluentAssertions;
using OpenSmc.Activities;
using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Ifrs17.Utils;
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
                   .ConfigureReferenceDataModelHub();
    }

    protected override MessageHubConfiguration ConfigureClient(MessageHubConfiguration configuration)
        => base.ConfigureClient(configuration)
            .AddData(dc => dc
                .FromHub(new ReferenceDataAddress(new HostAddress()), ds => ds
                    .ConfigureTypesFromCategory(TemplateData.TemplateReferenceData)
                    .WithType<AocConfiguration>()
                    .WithType<PartitionByReportingNode>()
                    .WithType<PartitionByReportingNodeAndPeriod>()
                )
            );

    [Fact]
    public async Task InitReferenceDataTest()
    {
        var client = GetClient();

        //Get ActualCountPerType
        var actualCountsPerType = await GetActualCountsPerType(client, ExpectedCountPerType.Keys, new ReferenceDataAddress(new HostAddress()) );

        //Assert Count per Type
        actualCountsPerType.Should().Equal(ExpectedCountPerType);
    }
}


public class ReferenceDataImportTest(ITestOutputHelper output) : ReferenceDataIfrsHubTestBase(output)
{
    private const string LiabilityTypeData = @"@@LiabilityType
SystemName,DisplayName
NewSystemName,NewDisplayName
@@Novelty
SystemName,DisplayName
NewSystemName,NewDisplayName
@@PartitionByReportingNodeAndPeriod,
ReportingNode,Year,Month,Scenario,
CH,2050,12,,

";
    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
    {
        return base.ConfigureHost(configuration)
            .ConfigureReferenceDataModelHub()
            .ConfigureReferenceDataImportHub();
    }

    [Fact]
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

        //Assert changes Count
        var newExpectedCountPerType = ExpectedCountPerType.ToDictionary();
        newExpectedCountPerType[typeof(LiabilityType)] += 1;
        newExpectedCountPerType[typeof(Novelty)] += 1;
        newExpectedCountPerType[typeof(PartitionByReportingNodeAndPeriod)] += 1;

        //Wait for Import to finish before proceeding with the GetRequest
        await Task.Delay(300);
        actualCountsPerType = await GetActualCountsPerType(client, ExpectedCountPerType.Keys, new ReferenceDataAddress(new HostAddress())); //Test a type with Custom Key
        actualCountsPerType.Should().Equal(newExpectedCountPerType);

        //Assert data changed
        var workspace = client.GetWorkspace(); // TODO V10: client should be configured to become a member of DataSync flow (2024/03/21, Dmitry Kalabin)
        var liabilityTypes = await workspace.GetObservable<LiabilityType>().FirstAsync();
        var novelties = await workspace.GetObservable<Novelty>().FirstAsync();
        var partitions = await workspace.GetObservable<PartitionByReportingNodeAndPeriod>().FirstAsync();

        liabilityTypes.Where(x => x.SystemName == "NewSystemName" && x.DisplayName == "NewDisplayName").Should().HaveCount(1);
        novelties.Where(x => x.SystemName == "NewSystemName" && x.DisplayName == "NewDisplayName").Should().HaveCount(1);
        partitions.Where(x => x.Year == 2050).Should().HaveCount(1);
    }
}

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
        { typeof(ReportingNode), 8},
        { typeof(PartitionByReportingNode), 1},
        { typeof(PartitionByReportingNodeAndPeriod), 1},
    };
}
