using System.Reflection;
using FluentAssertions;
using OpenSmc.Activities;
using OpenSmc.Data;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Import;
using OpenSmc.Messaging;
using OpenSmc.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.ReferenceDataHub.Test;

public class ImportReferenceDataTest(ITestOutputHelper output) : HubTestBase(output)
{
    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
    {
        return base.ConfigureHost(configuration)
            .AddImport(import => import)
            .AddData(dc => dc
                .WithDataSource("ReferenceDataSource",
                    ds => ds.ConfigureCategory(TemplateData.TemplateReferenceData)
                            .ConfigureCategory(ReferenceDataHubConfiguration.ReferenceDataDomainExtra)));
    }

    private static readonly MethodInfo AwaitResponseMethod = ReflectionHelper.GetMethodGeneric<IMessageHub>(x => x.AwaitResponse<object>(null, null));

    private static readonly Dictionary<Type, int> expectedCountPerType = new()
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


    [Fact]
    public async Task ImportDimensionTest()
    {
        // arrange
        var client = GetClient();
        var importRequest = new ImportRequest(TemplateDimensions.Csv);

        // act
        var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new HostAddress()));

        //assert
        importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);

        var allDomainTypes = ReferenceDataHubConfiguration.ReferenceDataDomain.Keys
            .Concat(ReferenceDataHubConfiguration.ReferenceDataDomainExtra.Select(td => td.GetType().GetGenericArguments().First()))
            .ToArray();
        var actualCountsPerType = new Dictionary<Type, int>();
        foreach (var domainType in allDomainTypes)
        {
            var requestType = typeof(GetManyRequest<>).MakeGenericType(domainType);
            var request = Activator.CreateInstance(requestType);
            var responseType = typeof(GetManyResponse<>).MakeGenericType(domainType);
            Func<PostOptions, PostOptions> options = o => o.WithTarget(new HostAddress());
            object response = (((IMessageDelivery)await AwaitResponseMethod.MakeGenericMethod(responseType).InvokeAsFunctionAsync(client, request, options)).Message);
            var total = ((GetManyResponseBase)response).Total;
            actualCountsPerType[domainType] = total;
        }

        actualCountsPerType.Should().Equal(expectedCountPerType);
    }
}
