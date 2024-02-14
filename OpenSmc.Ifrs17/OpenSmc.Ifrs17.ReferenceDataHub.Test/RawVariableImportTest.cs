﻿using System;
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
ReportingNode,Year,Quarter,Scenario,DataNode,AmountType,EstimateType,AocType,Novelty,AccidentYear,CashFlowPeriodicity,InterpolationMethod,Value
CH,2020,12,,DT10.2,DAE,BE,BOP,N,,Monthly,Uniform,1000,0,1300
CH,2020,12,,DT10.2,PR,BE,BOP,N,,Monthly,Uniform,1000,";

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
            ReportingNode = "DE",
            Year = 2020,
            Month = 12, 
            Scenario = null,
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
        rawVariableItems.Message.Items.Count.Should().Be(2);
    }
}

