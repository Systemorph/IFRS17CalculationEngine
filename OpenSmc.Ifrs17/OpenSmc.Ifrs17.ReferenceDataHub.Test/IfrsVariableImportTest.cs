using FluentAssertions;
using OpenSmc.Activities;
using OpenSmc.Data;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;
using OpenSmc.Import;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.ReferenceDataHub.Test
{
    public class IfrsVariableImportTest(ITestOutputHelper output) : HubTestBase(output)
    {
        private const string ifrsVariableCsv =
            @"@@IfrsVariable
ReportingNode,Year,Quarter,Scenario,DataNode,AmountType,EstimateType,AocType,Novelty,AccidentYear,Value0,Value1,Value2,Value3
CH,2020,12,,DT10,DAE,BE,BOP,N,,1000,1000,1000,1000
CH,2020,12,,DT10,PR,BE,BOP,N,,1000,1000,1000,1000";


        protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration) =>
            base.ConfigureHost(configuration).AddData(data => data.WithDataSource(nameof(DataSource),
                    source => source.WithType<IfrsVariable>(t => t.WithKey(x =>
                            (x.ReportingNode, x.Year, x.AmountType, x.Novelty, x.Month, x.DataNode, x.EstimateType, x.EconomicBasis))
                        .WithInitialData(_referenceIfrsVariable[typeof(IfrsVariable)].Cast<IfrsVariable>()))))
                .AddImport(imp => imp);

        private readonly Dictionary<Type, IEnumerable<object>> _referenceIfrsVariable =
            new()
            {
                {
                    typeof(IfrsVariable), new[]
                    {
                        new IfrsVariable()
                        {
                            EstimateType = "BE",
                            EconomicBasis = "L",
                            DataNode = "DT10.2",
                            AmountType = "DAE",
                            AocType = "BOP",
                            Novelty = "N",
                            ReportingNode = "DE",
                            Year = 2020,
                            Month = 12,
                            Scenario = null,
                            Values = [1000.0,1000,1000,1000]
                        }
                    }
                }
            };

        [Fact]
        public async Task ImportCashflowsTest()
        {
            var client = GetClient();
            var importRequest = new ImportRequest(ifrsVariableCsv);
            var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new HostAddress()));
            importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);
            var ifrsVariableItems = await client.AwaitResponse(new GetManyRequest<IfrsVariable>(),
                o => o.WithTarget(new HostAddress()));
            ifrsVariableItems.Message.Items.Count.Should().Be(3);
        }

    }
}