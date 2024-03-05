using FluentAssertions;
using OpenSmc.Ifrs17.DataNodeHub;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.Hub.Test;

public class DataNodeDataDictInitTest(ITestOutputHelper output) : DataNodeDataIfrsHubTestBase(output)
{
    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
    {
        return base.ConfigureHost(configuration)
            .ConfigureDataNodeDataDictInit();
    }

    [Fact]
    public async Task InitDataNodeDictInitTest()
    {
        var client = GetClient();

        //Get ActualCountPerType
        var actualCountsPerType = await GetActualCountsPerType(client, ExpectedCountPerType.Keys, new HostAddress());

        //Assert Count per Type
        actualCountsPerType.Should().Equal(ExpectedCountPerType);
    }
}

public class DataNodeDataIfrsHubTestBase(ITestOutputHelper output) : IfrsHubTestBase(output)
{

    internal static readonly Dictionary<Type, int> ExpectedCountPerType = new()
    {
        { typeof(InsurancePortfolio), 7 },
        { typeof(ReinsurancePortfolio), 3 },
        { typeof(GroupOfContract), 13 },
        { typeof(GroupOfReinsuranceContract), 6 },
    };
}