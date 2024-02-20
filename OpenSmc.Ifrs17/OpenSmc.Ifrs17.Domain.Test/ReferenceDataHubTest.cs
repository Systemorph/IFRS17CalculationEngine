using FluentAssertions;
using OpenSmc.Data;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;


namespace OpenSmc.Ifrs17.Domain.Test;
public class ReferenceDataHubTest(ITestOutputHelper output) : HubTestBase(output)
{

    private readonly TestReferenceData _testReferenceData = new();

    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
        => base.ConfigureHost(configuration).AddData(dc => dc.WithDataSource("ReferenceDataSource", 
            ds => ds.WithType<AmountType>(t => t.WithKey(x => x.SystemName)
                .WithInitialData(async () => await Task.FromResult(_testReferenceData.ReferenceAmountTypes))
                .WithUpdate(AddAmountType)
                .WithAdd(AddAmountType)
                .WithDelete(DeleteAmountType))
                .WithType<AocStep>(t => t.WithKey(x => (x.AocType, x.Novelty))
                    .WithInitialData(async () => await Task.FromResult(_testReferenceData.ReferenceAocSteps))
                    .WithUpdate(AddAocStep)
                    .WithAdd(AddAocStep)
                    .WithDelete(DeleteAocStep))));

    private void DeleteAocStep(IReadOnlyCollection<AocStep> obj)
    {
        _testReferenceData.ReferenceAocSteps = _testReferenceData.ReferenceAocSteps
            .Where(x => !obj.Contains(x))
            .ToArray();
    }

    private void AddAocStep(IReadOnlyCollection<AocStep> obj)
    {
        _testReferenceData.ReferenceAocSteps = _testReferenceData.ReferenceAocSteps
            .Concat(obj)
            .Distinct()
            .ToArray();
    }

    private void DeleteAmountType(IReadOnlyCollection<AmountType> obj)
    { 
        _testReferenceData.ReferenceAmountTypes = _testReferenceData.ReferenceAmountTypes
            .Where(x => !obj.Contains(x))
            .ToArray();
    }

    private void AddAmountType(IReadOnlyCollection<AmountType> obj)
    {
        _testReferenceData.ReferenceAmountTypes = _testReferenceData.ReferenceAmountTypes
            .Concat(obj)
            .Distinct()
            .ToArray();
    }

    [Fact]
    public async Task InitializationRdhAocTest()
    {
        _testReferenceData.Reset();
        var client = GetClient();
        var response = await client.AwaitResponse(new GetManyRequest<AocStep>(), o => o.WithTarget(new HostAddress()));
        var expected = new GetManyResponse<AocStep>(_testReferenceData.ReferenceAocSteps.Length, 
            _testReferenceData.ReferenceAocSteps);
        response.Message.Should().BeAssignableTo<GetManyResponse<AocStep>>();
        response.Message.Total.Should().Be(expected.Total);
        response.Message.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task InitializationRdhAmountTypeTest()
    {
        _testReferenceData.Reset();
        var client = GetClient();
        var response = await client.AwaitResponse(new GetManyRequest<AmountType>(),
            o => o.WithTarget(new HostAddress()));
        var expected = new GetManyResponse<AmountType>(_testReferenceData.ReferenceAmountTypes.Length,
            _testReferenceData.ReferenceAmountTypes);
        response.Message.Should().BeAssignableTo<GetManyResponse<AmountType>>();
        response.Message.Total.Should().Be(expected.Total);
        response.Message.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task UpdateRdhAmountTypeTest()
    {
        var updateItems = new AmountType[]
        {
            new AmountType{ SystemName = "W", DisplayName = "WriteOff", Parent = "", Order = 10, PeriodType = PeriodType.BeginningOfPeriod }
            
        };
        var client = GetClient();
        var updateResponse = await client.AwaitResponse(new UpdateDataRequest(updateItems), 
            o => o.WithTarget(new HostAddress()));
        await Task.Delay(300);
        var expected = new DataChanged(1);
        updateResponse.Message.Should().BeEquivalentTo(expected);
        _testReferenceData.ReferenceAmountTypes.Should().Contain(updateItems);
    }

    [Fact]
    public async Task DeleteRdhAmountTypeTest()
    {
        _testReferenceData.Reset();
        var deleteItems = new AmountType[]
        {
            new AmountType{ SystemName = "E", DisplayName = "Expenses", Parent = "", Order = 10, PeriodType = PeriodType.BeginningOfPeriod }
        };
        var client = GetClient();
        var deleteResponse = await client.AwaitResponse(new DeleteDataRequest(deleteItems),
            o => o.WithTarget(new HostAddress()));
        await Task.Delay(300);
        var expected = new DataChanged(1);
        deleteResponse.Message.Should().BeEquivalentTo(expected);
        _testReferenceData.ReferenceAmountTypes.Should().NotContain(deleteItems);
    }


}
