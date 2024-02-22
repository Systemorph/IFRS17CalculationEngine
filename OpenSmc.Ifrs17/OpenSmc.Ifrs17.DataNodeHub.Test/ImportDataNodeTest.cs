using OpenSmc.Data;
using OpenSmc.Hub.Fixture;
using Xunit;
using Xunit.Abstractions;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReferenceDataHub.Test;

public class ImportDataNodeTest(ITestOutputHelper output) : HubTestBase(output)
{
    public static readonly Dictionary<Type, IEnumerable<object>> DataNodeDomain
        =
        new()
        {
            { typeof(InsurancePortfolio), new InsurancePortfolio[] { } },
            { typeof(GroupOfInsuranceContract), new GroupOfInsuranceContract[] { } },
            { typeof(ReinsurancePortfolio), new ReinsurancePortfolio[] { } },
            { typeof(GroupOfReinsuranceContract), new GroupOfReinsuranceContract[] { } },
        };

    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
    {
        return base.ConfigureHost(configuration);//.ConfigureDataNodes(DataNodeDomain);
    }

    [Fact]
    public async Task ImportDimensionTest()
    {
        var client = GetClient();
        //var importRequest = new ImportRequest(TemplateDimensions.Csv);
        //var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new HostAddress()));
        //importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);

        var atItems = await client.AwaitResponse(new GetManyRequest<InsurancePortfolio>());
    }
}