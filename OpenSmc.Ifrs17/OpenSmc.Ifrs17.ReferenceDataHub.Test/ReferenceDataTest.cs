using FluentAssertions;
using OpenSmc.Activities;
using OpenSmc.Data;
using OpenSmc.Hub.Fixture;
using OpenSmc.Import;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.DataTypes.Constants;

namespace OpenSmc.Ifrs17.ReferenceDataHub.Test;

public class ImportReferenceDataTest(ITestOutputHelper output) : HubTestBase(output)
    {
        public static readonly Dictionary<Type, IEnumerable<object>> ReferenceDataDomain
            =
            new()
            {
                { typeof(AmountType), Array.Empty<AmountType>() },
                { typeof(DeferrableAmountType), new DeferrableAmountType[] {} },
                { typeof(AocType), new AocType[] {} },
                { typeof(AocConfiguration), new AocConfiguration[] {} },
                { typeof(StructureType), new StructureType[] {} },
                { typeof(CreditDefaultRate), new CreditDefaultRate[] {} },
                { typeof(Currency), new Currency[] {} },
                { typeof(EconomicBasis), new EconomicBasis[] {} },
                { typeof(EstimateType), new EstimateType[] {} },
                { typeof(LiabilityType), new LiabilityType[] {} },
                { typeof(LineOfBusiness), new LineOfBusiness[] {} },
                { typeof(Novelty), new  Novelty[] {} },
                { typeof(OciType), new  OciType[] {} },
                { typeof(Partner), new  Partner[] {} },
                { typeof(BsVariableType), new  BsVariableType[] {} },
                { typeof(PnlVariableType), new  PnlVariableType[] {} },
                { typeof(RiskDriver), new  RiskDriver[] {} },
                { typeof(Scenario), new  Scenario[] {} },
                { typeof(ValuationApproach), new  ValuationApproach[] {} },
                { typeof(ProjectionConfiguration), new  ProjectionConfiguration[] {} },
            };

        protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
        {
            return base.ConfigureHost(configuration)
                .AddData(data => data.WithDataSource(nameof(DataSource), 
                        source => source.ConfigureCategory(ReferenceDataDomain)))
                .AddImport(import => import);
        }

        [Fact]
        public async Task ImportDimensionTest()
        {
            var client = GetClient();
            var importRequest = new ImportRequest(TemplateDimensions.Csv);
            var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new HostAddress()));
            importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);

            var atItems = await client.AwaitResponse(new GetManyRequest<AmountType>(),
                o => o.WithTarget(new HostAddress()));
            var datItems = await client.AwaitResponse(new GetManyRequest<DeferrableAmountType>(),
                o => o.WithTarget(new HostAddress()));
            var aocItems = await client.AwaitResponse(new GetManyRequest<AocType>(),
                o => o.WithTarget(new HostAddress()));
            var aoccItems = await client.AwaitResponse(new GetManyRequest<AocConfiguration>(),
                o => o.WithTarget(new HostAddress()));
            var stItems = await client.AwaitResponse(new GetManyRequest<StructureType>(),
                o => o.WithTarget(new HostAddress()));
            var crrItems = await client.AwaitResponse(new GetManyRequest<CreditRiskRating>(),
                o => o.WithTarget(new HostAddress()));
            var cItems = await client.AwaitResponse(new GetManyRequest<Currency>(),
                o => o.WithTarget(new HostAddress()));
            var ecItems = await client.AwaitResponse(new GetManyRequest<EconomicBasis>(),
                o => o.WithTarget(new HostAddress()));
            var esItems = await client.AwaitResponse(new GetManyRequest<EstimateType>(),
                o => o.WithTarget(new HostAddress()));
            var ltItems = await client.AwaitResponse(new GetManyRequest<LiabilityType>(),
                o => o.WithTarget(new HostAddress()));
            var lobItems = await client.AwaitResponse(new GetManyRequest<LineOfBusiness>(),
                o => o.WithTarget(new HostAddress()));
            var nItems = await client.AwaitResponse(new GetManyRequest<Novelty>(),
                o => o.WithTarget(new HostAddress()));
            var otItems = await client.AwaitResponse(new GetManyRequest<OciType>(),
                o => o.WithTarget(new HostAddress()));
            var pItemsn = await client.AwaitResponse(new GetManyRequest<Partner>(),
                o => o.WithTarget(new HostAddress()));
            var bvtItems = await client.AwaitResponse(new GetManyRequest<BsVariableType>(),
                o => o.WithTarget(new HostAddress()));
            var pvt = await client.AwaitResponse(new GetManyRequest<PnlVariableType>(),
                o => o.WithTarget(new HostAddress()));
            var rdItems = await client.AwaitResponse(new GetManyRequest<RiskDriver>(),
                o => o.WithTarget(new HostAddress()));
            var sItems = await client.AwaitResponse(new GetManyRequest<Scenario>(),
                o => o.WithTarget(new HostAddress()));
            var vaItems = await client.AwaitResponse(new GetManyRequest<ValuationApproach>(),
                o => o.WithTarget(new HostAddress()));
            var pcItems = await client.AwaitResponse(new GetManyRequest<ProjectionConfiguration>(),
                o => o.WithTarget(new HostAddress()));

            atItems.Message.Items.Should().HaveCount(17);
            datItems.Message.Items.Should().HaveCount(2);
            aocItems.Message.Items.Should().HaveCount(17);
            aoccItems.Message.Items.Should().HaveCount(20);
        }
}
