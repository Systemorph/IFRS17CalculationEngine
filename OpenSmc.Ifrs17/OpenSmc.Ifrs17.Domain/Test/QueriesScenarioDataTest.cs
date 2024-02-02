using FluentAssertions;
using OpenSmc.Collections;
using OpenSmc.DataSource.Abstractions;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.DataModel.TransactionalData;
using OpenSmc.Ifrs17.Domain.Report;
using OpenSmc.Ifrs17.Domain.Tests;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Scopes.Proxy;
using OpenSmc.Workspace;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Import;

namespace OpenSmc.Ifrs17.Domain.Tests;

public class QueriesScenarioDataTest : TestBase
{
    private RawVariable[] bestEstimateRawVars;
    private RawVariable[] previousScenarioRawVars;
    private RawVariable[] scenarioRawVars;
    private RawVariable[] newScenarioRawVars;
    private IfrsVariable[] bestEstimateIfrsVars;
    private IfrsVariable[] previousScenarioIfrsVars;
    private IfrsVariable[] scenarioIfrsVars;

    public QueriesScenarioDataTest(IImportVariable import, IDataSource dataSource,
        IWorkspaceVariable work, IActivityVariable activity, IScopeFactory scopes) :
        base(import, dataSource, work, activity, scopes){}

    private async Task InitializeAsync()
    {
        await TestData.InitializeAsync();
        await DataSource.DeleteAsync(DataSource.Query<RawVariable>());
        await DataSource.DeleteAsync(DataSource.Query<IfrsVariable>());
        await DataSource.UpdateAsync(TestData.partitionReportingNode.RepeatOnce());

        await DataSource.UpdateAsync(new[]
        {
            TestData.partition, TestData.previousPeriodPartition,
            TestData.partitionScenarioMTUP, TestData.previousPeriodPartitionScenarioMTUP
        });

        await DataSource.UpdateAsync(TestData.dt11.RepeatOnce());

        await Import.FromString(TestData.projectionConfiguration)
            .WithType<ProjectionConfiguration>()
            //.WithTarget(DataSource)
            .ExecuteAsync();


        bestEstimateRawVars = new[]
        {
            new RawVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = TestData.partition.Id, Values = new[] {1.0}
            },
            new RawVariable
            {
                AmountType = "PR", AocType = "AU", Novelty = "I",
                Partition = TestData.partition.Id, Values = new[] {2.0}
            },
            new RawVariable
            {
                AmountType = "PR", AocType = "EV", Novelty = "I",
                Partition = TestData.partition.Id, Values = new[] {3.0}
            },
            new RawVariable
            {
                AmountType = "CL", AocType = "CL", Novelty = "I",
                Partition = TestData.partition.Id, Values = new[] {4.0}
            },
            new RawVariable
            {
                AmountType = "CL", AocType = "AU", Novelty = "I",
                Partition = TestData.partition.Id, Values = new[] {5.0}
            },
            new RawVariable
            {
                AmountType = "CL", AocType = "EV", Novelty = "I",
                Partition = TestData.partition.Id, Values = new[] {6.0}
            }
        };


        previousScenarioRawVars = new[]
        {
            new RawVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = TestData.previousPeriodPartitionScenarioMTUP.Id,
                Values = new[] {3.15}
            },
            new RawVariable
            {
                AmountType = "PR", AocType = "AU", Novelty = "I", Partition =
                    TestData.previousPeriodPartitionScenarioMTUP.Id,
                Values = new[] {7.17}
            }
        };


        scenarioRawVars = new[]
        {
            new RawVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = TestData.partitionScenarioMTUP.Id,
                Values = new[] {1.1}
            },
            new RawVariable
            {
                AmountType = "PR", AocType = "AU", Novelty = "I",
                Partition = TestData.partitionScenarioMTUP.Id,
                Values = new[] {2.1}
            }
        };


        newScenarioRawVars = new[]
        {
            new RawVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = TestData.partitionScenarioMTUP.Id,
                Values = new[] {11.0}
            },
            new RawVariable
            {
                AmountType = "CL", AocType = "CL", Novelty = "I",
                Partition = TestData.partitionScenarioMTUP.Id,
                Values = new[] {41.0}
            }
        };
    }

    public async Task Test1Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<RawVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateRawVars.Concat(scenarioRawVars).ToArray());
        var ws = Work.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());
        await ws.UpdateAsync(newScenarioRawVars);


        var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable, PartitionByReportingNodeAndPeriod>(
            DataSource,
            TestData.partitionScenarioMTUP.Id, TestData.partition.Id, ImportFormats.Cashflow);


        queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(52.0);
    }

    public async Task Test2Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<RawVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateRawVars.Concat(scenarioRawVars).ToArray());
        var ws = Work.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable, PartitionByReportingNodeAndPeriod>(
            DataSource,
            TestData.partitionScenarioMTUP.Id, TestData.partition.Id, ImportFormats.Actual);


        queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(0);
    }

    public async Task Test3Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<RawVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateRawVars.Concat(scenarioRawVars).ToArray());
        var ws = Work.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable, PartitionByReportingNodeAndPeriod>(
            DataSource,
            TestData.partitionScenarioMTUP.Id, TestData.partition.Id, ImportFormats.Cashflow);


        queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(3.2);
    }

    public async Task Test4Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<RawVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateRawVars.ToArray());
        var ws = Work.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable, PartitionByReportingNodeAndPeriod>(
            DataSource,
            TestData.partitionScenarioMTUP.Id, TestData.partition.Id, ImportFormats.Cashflow);


        queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(21);
    }

    public async Task Test5Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<RawVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateRawVars.Concat(previousScenarioRawVars).ToArray());
        var ws = Work.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable, PartitionByReportingNodeAndPeriod>(
            DataSource,
            TestData.partitionScenarioMTUP.Id, TestData.partition.Id, ImportFormats.Cashflow);


        queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(21);
    }

    public async Task Test6Async()
    {
        bestEstimateIfrsVars = new[]
        {
            new IfrsVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I", Partition = TestData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(1.0)
            },
            new IfrsVariable
            {
                AmountType = "PR", AocType = "AU", Novelty = "I", Partition = TestData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(2.0)
            },
            new IfrsVariable
            {
                AmountType = "PR", AocType = "EV", Novelty = "I", Partition = TestData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(3.0)
            },
            new IfrsVariable
            {
                AmountType = "CL", AocType = "CL", Novelty = "I", Partition = TestData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(4.0)
            },
            new IfrsVariable
            {
                AmountType = "CL", AocType = "AU", Novelty = "I", Partition = TestData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(5.0)
            },
            new IfrsVariable
            {
                AmountType = "CL", AocType = "EV", Novelty = "I", Partition = TestData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(6.0)
            }
        };


        previousScenarioIfrsVars = new[]
        {
            new IfrsVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = TestData.previousPeriodPartitionScenarioMTUP.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(3.15)
            },
            new IfrsVariable
            {
                AmountType = "PR", AocType = "AU", Novelty = "I",
                Partition = TestData.previousPeriodPartitionScenarioMTUP.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(7.17)
            }
        };


        scenarioIfrsVars = new[]
        {
            new IfrsVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = TestData.partitionScenarioMTUP.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(1.1)
            },
            new IfrsVariable
            {
                AmountType = "PR", AocType = "AU", Novelty = "I",
                Partition = TestData.partitionScenarioMTUP.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(2.1)
            }
        };


        var newScenarioIfrsVars = new[]
        {
            new IfrsVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = TestData.partitionScenarioMTUP.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(11.0)
            },
            new IfrsVariable
            {
                AmountType = "CL", AocType = "CL", Novelty = "I",
                Partition = TestData.partitionScenarioMTUP.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(41.0)
            }
        };


        await DataSource.DeleteAsync(await DataSource.Query<IfrsVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateIfrsVars.Concat(scenarioIfrsVars).ToArray());
        var ws = Work.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());
        await ws.UpdateAsync(newScenarioIfrsVars);


        var queriedIfrsVars =
            await ws.QueryPartitionedDataAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(DataSource,
                TestData.partitionScenarioMTUP.Id, TestData.partition.Id, ImportFormats.Actual);


        queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(52.0);
    }

    public async Task Test7Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<IfrsVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateIfrsVars.Concat(scenarioIfrsVars).ToArray());
        var ws = Work.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedIfrsVars =
            await ws.QueryPartitionedDataAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(DataSource,
                TestData.partitionScenarioMTUP.Id, TestData.partition.Id, ImportFormats.Cashflow);


        queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(0);
    }

    public async Task Test8Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<IfrsVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateIfrsVars.Concat(scenarioIfrsVars).ToArray());
        var ws = Work.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedIfrsVars =
            await ws.QueryPartitionedDataAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(DataSource,
                TestData.partitionScenarioMTUP.Id, TestData.partition.Id, ImportFormats.Actual);


        queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(3.2);
    }

    public async Task Test9Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<IfrsVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateIfrsVars.ToArray());
        var ws = Work.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedIfrsVars =
            await ws.QueryPartitionedDataAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(DataSource,
                TestData.partitionScenarioMTUP.Id, TestData.partition.Id, ImportFormats.Actual);


        queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(21.0);
    }

    public async Task Test10Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<IfrsVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateIfrsVars.Concat(previousScenarioIfrsVars).ToArray());
        var ws = Work.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedIfrsVars =
            await ws.QueryPartitionedDataAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(DataSource,
                TestData.partitionScenarioMTUP.Id, TestData.partition.Id, ImportFormats.Actual);


        queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(21);
    }

    public async Task Test11Async()
    {
        bestEstimateIfrsVars = new[]
        {
            new IfrsVariable
            {
                DataNode = "DT1.1", AmountType = "PR", AocType = "CL", Novelty = "I", 
                Partition = TestData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(1.0)
            },
            new IfrsVariable
            {
                DataNode = "DT1.1", AmountType = "PR", AocType = "AU", Novelty = "I", 
                Partition = TestData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(2.0)
            },
            new IfrsVariable
            {
                DataNode = "DT1.1", AmountType = "PR", AocType = "EV", Novelty = "I", 
                Partition = TestData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(3.0)
            },
            new IfrsVariable
            {
                DataNode = "DT1.1", AmountType = "CL", AocType = "CL", Novelty = "I", 
                Partition = TestData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(4.0)
            },
            new IfrsVariable
            {
                DataNode = "DT1.1", AmountType = "CL", AocType = "AU", Novelty = "I", 
                Partition = TestData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(5.0)
            },
            new IfrsVariable
            {
                DataNode = "DT1.1", AmountType = "CL", AocType = "EV", Novelty = "I", 
                Partition = TestData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(6.0)
            }
        };


    scenarioIfrsVars = new[]
    {
        new IfrsVariable
        {
            DataNode = "DT1.1", AmountType = "PR", AocType = "CL", Novelty = "I", 
            Partition = TestData.partitionScenarioMTUP.Id,
            Values = ImportCalculationExtensions.SetProjectionValue(1.1)
        },
        new IfrsVariable
        {
            DataNode = "DT1.1", AmountType = "PR", AocType = "AU", Novelty = "I", 
            Partition = TestData.partitionScenarioMTUP.Id,
            Values = ImportCalculationExtensions.SetProjectionValue(2.1)
        }
    };


    var ws = Work.CreateNew();
    ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


    await ws.UpdateAsync(bestEstimateIfrsVars);
    await ws.UpdateAsync(scenarioIfrsVars);


    var projectionConfigurations =
        (await ws.Query<ProjectionConfiguration>().ToArrayAsync())
        .SortRelevantProjections(TestData.args.Month);


    (await ws.QueryReportVariablesAsync((TestData.args.Year, TestData.args.Month, 
            TestData.args.ReportingNode, TestData.args.Scenario),
        projectionConfigurations)).Select(x => x.Value).Sum().Should().Be(21);
}

    public async Task Test12Async()
    {
        var ws = Work.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        await ws.UpdateAsync(bestEstimateIfrsVars);
        await ws.UpdateAsync(scenarioIfrsVars);


        var projectionConfigurations =
            (await ws.Query<ProjectionConfiguration>().ToArrayAsync())
            .SortRelevantProjections(TestData.args.Month);


        (await ws.QueryReportVariablesAsync((TestData.args.Year, TestData.args.Month, 
                TestData.args.ReportingNode, TestData.argsScenarioMTUP.Scenario),
            projectionConfigurations)).Select(x => x.Value).Sum().Should().Be(21.2);
    }

}

