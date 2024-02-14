using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using OpenSmc.Activities;
using OpenSmc.Data;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;
using OpenSmc.Import;
using OpenSmc.Import.Test;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.ReferenceDataHub.Test;

public class RawVariableImportTest(ITestOutputHelper output) : HubTestBase(output) 
{
    private const string cashFlowCsv =
        @"@@RawVariable,,,,,,,,,,,,,,,,,,,,,,,,,,,,,
ReportingNode,Year,Quarter,Scenario,DataNode,AmountType,EstimateType,AocType,Novelty,AccidentYear,CashFlowPeriodicity,InterpolationMethod,Values0,Values1,Values2,Values3,Values4,Values5,Values6,Values7,Values8,Values9,Values10,Values11,Values12,Values13,Values14,Values15,Values16,Values17,Values18,Values19,Values20,Values21,Values22,Values23
DT10.2,DAE,BE,BOP,N,,Monthly,Uniform,1000,0,1300
DT10.2,PR,BE,BOP,N,,Monthly,Uniform,1000,";

    protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration) =>
        base.ConfigureHost(configuration).AddData(data => data.WithDataSource(nameof(DataSource), 
            source => source.ConfigureCategory(_referenceRawVariable)))
            .AddImport(imp => imp);

    private readonly Dictionary<Type, IEnumerable<object>> _referenceRawVariable = 
        new() {{typeof(RawVariable), new[]{new RawVariable()
        {
            EstimateType = "BE",
            DataNode = "DT10.2",
            AmountType = "DAE",
            AocType = "BOP", 
            Novelty = "N", 
            Value = 15
        }}}};

    [Fact]
    public async Task ImportCashflowsTest()
    {
        var client = GetClient();
        var importRequest = new ImportRequest(cashFlowCsv);
        var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new HostAddress()));
        importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);
        var rawVariableItems = await client.AwaitResponse(new GetManyRequest<RawVariable>(), 
            o => o.WithTarget(new HostAddress()));
    }
}

