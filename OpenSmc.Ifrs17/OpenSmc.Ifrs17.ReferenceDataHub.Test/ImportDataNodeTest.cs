using OpenSmc.Data;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.Hub.Test;

public class DataNodeImportTest(ITestOutputHelper output) : HubTestBase(output)
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

    [Fact(Skip = "Implementation of the Hub and of the Test are pending.")]
    public async Task ImportDataTest()
    {
        var client = GetClient();
        //var importRequest = new ImportRequest(TemplateDimensions.Csv);
        //var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new HostAddress()));
        //importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);

        var atItems = await client.AwaitResponse(new GetManyRequest<InsurancePortfolio>());
    }
}