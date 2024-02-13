using FluentAssertions;
using OpenSmc.Data;
using OpenSmc.Domain.Abstractions;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;


namespace OpenSmc.Ifrs17.Domain.Test;
public class ReferenceDataHubTest(ITestOutputHelper output) : HubTestBase(output)
{

    private readonly ReferenceData _referenceData = new();

    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
        => base.ConfigureHost(configuration).AddData(dc => dc.WithDataSource("ReferenceDataSource", 
            ds => ds.WithType<AmountType>(t => t.WithKey(x => x.SystemName)
                .WithInitialization(async () => await Task.FromResult(_referenceData.ReferenceAmountTypes))
                .WithUpdate(AddAmountType)
                .WithAdd(AddAmountType)
                .WithDelete(DeleteAmountType))
                .WithType<AocStep>(t => t.WithKey(x => (x.AocType, x.Novelty))
                    .WithInitialization(async () => await Task.FromResult(_referenceData.ReferenceAocSteps))
                    .WithUpdate(AddAocStep)
                    .WithAdd(AddAocStep)
                    .WithDelete(DeleteAocStep))));

    private void DeleteAocStep(IReadOnlyCollection<AocStep> obj)
    {
        throw new NotImplementedException();
    }

    private void AddAocStep(IReadOnlyCollection<AocStep> obj)
    {
        throw new NotImplementedException();
    }

    private void DeleteAmountType(IReadOnlyCollection<AmountType> obj)
    {
        throw new NotImplementedException();
    }

    private void AddAmountType(IReadOnlyCollection<AmountType> obj)
    {
        throw new NotImplementedException();
    }

    [Fact]
    public async Task InitializationRdhAocTest()
    {
        var client = GetClient();
        var response = await client.AwaitResponse(new GetManyRequest<AocStep>(), o => o.WithTarget(new HostAddress()));
        var expected = new GetManyResponse<AocStep>(_referenceData.ReferenceAocSteps.Length, 
            _referenceData.ReferenceAocSteps);
        response.Message.Should().BeAssignableTo<GetManyResponse<AocStep>>();
        response.Message.Total.Should().Be(expected.Total);
        foreach (var element in expected.Items.Select(x => (x.AocType, x.Novelty)))
            response.Message.Items.Select(x => (x.AocType, x.Novelty)).Should().Contain(element);
    }


    [Fact]
    public async Task InitializationRdhAmountTypeTest()
    {
        var client = GetClient();
        var response = await client.AwaitResponse(new GetManyRequest<AmountType>(),
            o => o.WithTarget(new HostAddress()));
        var expected = new GetManyResponse<AmountType>(_referenceData.ReferenceAmountTypes.Length,
            _referenceData.ReferenceAmountTypes);
        response.Message.Should().BeAssignableTo<GetManyResponse<AmountType>>();
        response.Message.Total.Should().Be(expected.Total);
        foreach (var element in expected.Items.Select(x => new Dimension(){SystemName = x.SystemName, DisplayName = x.DisplayName}))
        {
            response.Message.Items
                .Select(x => new Dimension(){SystemName = x.SystemName, DisplayName = x.DisplayName})
                .Should().Contain(element);
        }
    }

    [Fact]
    public async Task UpdateRdhAmountTypeTest()
    {
        var updateItems = new AmountType[]
        {
            new()
            {
                SystemName = "W", DisplayName = "WriteOff"

            }
        };
        var client = GetClient();
        var updateResponse = await client.AwaitResponse(new UpdateDataRequest(updateItems), 
            o => o.WithTarget(new HostAddress()));
        await Task.Delay(300);
        var expected = new DataChanged(1);
        updateResponse.Message.Should().BeEquivalentTo(expected);
        DataHubConfiguration.GetAmountTypes().Select(x => new Dimension()
        {
            SystemName = x.SystemName,
            DisplayName = x.DisplayName
        }).Should().Contain(updateItems.Select(x => new Dimension()
        {
            SystemName = x.SystemName,
            DisplayName = x.DisplayName
        }).FirstOrDefault() ?? throw new Exception("Element not found"));
    }

    [Fact]
    public async Task DeleteRdhAmountTypeTest()
    {
        var deleteItems = new AmountType[]
        {
            new()
            {
                SystemName = "E",
                DisplayName = "Expenses"
            }
        };
        var client = GetClient();
        var deleteResponse = await client.AwaitResponse(new DeleteDataRequest(deleteItems),
            o => o.WithTarget(new HostAddress()));
        await Task.Delay(300);
        var expected = new DataChanged(1);
        deleteResponse.Message.Should().BeEquivalentTo(expected);
        DataHubConfiguration.GetAmountTypes().Select(x => new Dimension()
        {
            SystemName = x.SystemName,
            DisplayName = x.DisplayName
        }).Should().NotContain(deleteItems.Select(x => new Dimension()
        {
            SystemName = x.SystemName,
            DisplayName = x.DisplayName
        }).FirstOrDefault() ?? throw new Exception("Delete element not found"));
    }


}
