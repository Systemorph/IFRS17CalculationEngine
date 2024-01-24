//#!import "../Utils/Queries"
//#!import "../Utils/ImportCalculationMethods"
//#!import "../Report/ReportConfigurationAndUtils"
//#!import "TestData"

using FluentAssertions;
using Microsoft.Graph.SecurityNamespace;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Report;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Import;
using Systemorph.Vertex.Workspace;

public class QueriesScenarioDataTest
{
    protected IImportVariable Import;
    protected IDataSource DataSource;
    protected IWorkspaceVariable Workspace;
    protected TestData testData;
    protected IActivityVariable Activity;
    private RawVariable[] bestEstimateRawVars;
    private RawVariable[] previousScenarioRawVars;
    private RawVariable[] scenarioRawVars;
    private RawVariable[] newScenarioRawVars;
    private IfrsVariable[] bestEstimateIfrsVars;
    private IfrsVariable[] previousScenarioIfrsVars;
    private IfrsVariable[] scenarioIfrsVars;

    public QueriesScenarioDataTest(IImportVariable import, IDataSource dataSource,
        IWorkspaceVariable workspace, IActivityVariable activity)
    {
        Import = import;
        DataSource = dataSource;
        Workspace = workspace;
        Activity = activity;
        testData = new TestData();
    }

    private async Task InitializeAsync()
    {
        testData.InitializeAsync();
        await DataSource.DeleteAsync(DataSource.Query<RawVariable>());
        await DataSource.DeleteAsync(DataSource.Query<IfrsVariable>());
        await DataSource.UpdateAsync(testData.partitionReportingNode.RepeatOnce());

        await DataSource.UpdateAsync(new[]
        {
            testData.partition, testData.previousPeriodPartition,
            testData.partitionScenarioMTUP, testData.previousPeriodPartitionScenarioMTUP
        });

        await DataSource.UpdateAsync(testData.dt11.RepeatOnce());

        await Import.FromString(testData.projectionConfiguration).WithType<ProjectionConfiguration>(
        ).WithTarget(DataSource).ExecuteAsync();


        bestEstimateRawVars = new[]
        {
            new RawVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = testData.partition.Id, Values = new[] {1.0}
            },
            new RawVariable
            {
                AmountType = "PR", AocType = "AU", Novelty = "I",
                Partition = testData.partition.Id, Values = new[] {2.0}
            },
            new RawVariable
            {
                AmountType = "PR", AocType = "EV", Novelty = "I",
                Partition = testData.partition.Id, Values = new[] {3.0}
            },
            new RawVariable
            {
                AmountType = "CL", AocType = "CL", Novelty = "I",
                Partition = testData.partition.Id, Values = new[] {4.0}
            },
            new RawVariable
            {
                AmountType = "CL", AocType = "AU", Novelty = "I",
                Partition = testData.partition.Id, Values = new[] {5.0}
            },
            new RawVariable
            {
                AmountType = "CL", AocType = "EV", Novelty = "I",
                Partition = testData.partition.Id, Values = new[] {6.0}
            }
        };


        previousScenarioRawVars = new[]
        {
            new RawVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = testData.previousPeriodPartitionScenarioMTUP.Id,
                Values = new[] {3.15}
            },
            new RawVariable
            {
                AmountType = "PR", AocType = "AU", Novelty = "I", Partition =
                    testData.previousPeriodPartitionScenarioMTUP.Id,
                Values = new[] {7.17}
            }
        };


        scenarioRawVars = new[]
        {
            new RawVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = testData.partitionScenarioMTUP.Id,
                Values = new[] {1.1}
            },
            new RawVariable
            {
                AmountType = "PR", AocType = "AU", Novelty = "I",
                Partition = testData.partitionScenarioMTUP.Id,
                Values = new[] {2.1}
            }
        };


        newScenarioRawVars = new[]
        {
            new RawVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = testData.partitionScenarioMTUP.Id,
                Values = new[] {11.0}
            },
            new RawVariable
            {
                AmountType = "CL", AocType = "CL", Novelty = "I",
                Partition = testData.partitionScenarioMTUP.Id,
                Values = new[] {41.0}
            }
        };
    }

    public async Task Test1Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<RawVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateRawVars.Concat(scenarioRawVars).ToArray());
        var ws = Workspace.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());
        await ws.UpdateAsync(newScenarioRawVars);


        var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable, PartitionByReportingNodeAndPeriod>(
            DataSource,
            testData.partitionScenarioMTUP.Id, testData.partition.Id, ImportFormats.Cashflow);


        queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(52.0);
    }

    public async Task Test2Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<RawVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateRawVars.Concat(scenarioRawVars).ToArray());
        var ws = Workspace.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable, PartitionByReportingNodeAndPeriod>(
            DataSource,
            testData.partitionScenarioMTUP.Id, testData.partition.Id, ImportFormats.Actual);


        queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(0);
    }

    public async Task Test3Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<RawVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateRawVars.Concat(scenarioRawVars).ToArray());
        var ws = Workspace.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable, PartitionByReportingNodeAndPeriod>(
            DataSource,
            testData.partitionScenarioMTUP.Id, testData.partition.Id, ImportFormats.Cashflow);


        queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(3.2);
    }

    public async Task Test4Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<RawVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateRawVars.ToArray());
        var ws = Workspace.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable, PartitionByReportingNodeAndPeriod>(
            DataSource,
            testData.partitionScenarioMTUP.Id, testData.partition.Id, ImportFormats.Cashflow);


        queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(21);
    }

    public async Task Test5Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<RawVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateRawVars.Concat(previousScenarioRawVars).ToArray());
        var ws = Workspace.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable, PartitionByReportingNodeAndPeriod>(
            DataSource,
            testData.partitionScenarioMTUP.Id, testData.partition.Id, ImportFormats.Cashflow);


        queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(21);
    }

    public async Task Test6Async()
    {
        bestEstimateIfrsVars = new[]
        {
            new IfrsVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I", Partition = testData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(1.0)
            },
            new IfrsVariable
            {
                AmountType = "PR", AocType = "AU", Novelty = "I", Partition = testData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(2.0)
            },
            new IfrsVariable
            {
                AmountType = "PR", AocType = "EV", Novelty = "I", Partition = testData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(3.0)
            },
            new IfrsVariable
            {
                AmountType = "CL", AocType = "CL", Novelty = "I", Partition = testData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(4.0)
            },
            new IfrsVariable
            {
                AmountType = "CL", AocType = "AU", Novelty = "I", Partition = testData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(5.0)
            },
            new IfrsVariable
            {
                AmountType = "CL", AocType = "EV", Novelty = "I", Partition = testData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(6.0)
            }
        };


        previousScenarioIfrsVars = new[]
        {
            new IfrsVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = testData.previousPeriodPartitionScenarioMTUP.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(3.15)
            },
            new IfrsVariable
            {
                AmountType = "PR", AocType = "AU", Novelty = "I",
                Partition = testData.previousPeriodPartitionScenarioMTUP.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(7.17)
            }
        };


        scenarioIfrsVars = new[]
        {
            new IfrsVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = testData.partitionScenarioMTUP.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(1.1)
            },
            new IfrsVariable
            {
                AmountType = "PR", AocType = "AU", Novelty = "I",
                Partition = testData.partitionScenarioMTUP.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(2.1)
            }
        };


        var newScenarioIfrsVars = new[]
        {
            new IfrsVariable
            {
                AmountType = "PR", AocType = "CL", Novelty = "I",
                Partition = testData.partitionScenarioMTUP.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(11.0)
            },
            new IfrsVariable
            {
                AmountType = "CL", AocType = "CL", Novelty = "I",
                Partition = testData.partitionScenarioMTUP.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(41.0)
            }
        };


        await DataSource.DeleteAsync(await DataSource.Query<IfrsVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateIfrsVars.Concat(scenarioIfrsVars).ToArray());
        var ws = Workspace.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());
        await ws.UpdateAsync(newScenarioIfrsVars);


        var queriedIfrsVars =
            await ws.QueryPartitionedDataAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(DataSource,
                testData.partitionScenarioMTUP.Id, testData.partition.Id, ImportFormats.Actual);


        queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(52.0);
    }

    public async Task Test7Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<IfrsVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateIfrsVars.Concat(scenarioIfrsVars).ToArray());
        var ws = Workspace.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedIfrsVars =
            await ws.QueryPartitionedDataAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(DataSource,
                testData.partitionScenarioMTUP.Id, testData.partition.Id, ImportFormats.Cashflow);


        queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(0);
    }

    public async Task Test8Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<IfrsVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateIfrsVars.Concat(scenarioIfrsVars).ToArray());
        var ws = Workspace.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedIfrsVars =
            await ws.QueryPartitionedDataAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(DataSource,
                testData.partitionScenarioMTUP.Id, testData.partition.Id, ImportFormats.Actual);


        queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(3.2);
    }

    public async Task Test9Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<IfrsVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateIfrsVars.ToArray());
        var ws = Workspace.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedIfrsVars =
            await ws.QueryPartitionedDataAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(DataSource,
                testData.partitionScenarioMTUP.Id, testData.partition.Id, ImportFormats.Actual);


        queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(21.0);
    }

    public async Task Test10Async()
    {
        await DataSource.DeleteAsync(await DataSource.Query<IfrsVariable>().ToArrayAsync());
        await DataSource.UpdateAsync(bestEstimateIfrsVars.Concat(previousScenarioIfrsVars).ToArray());
        var ws = Workspace.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        var queriedIfrsVars =
            await ws.QueryPartitionedDataAsync<IfrsVariable, PartitionByReportingNodeAndPeriod>(DataSource,
                testData.partitionScenarioMTUP.Id, testData.partition.Id, ImportFormats.Actual);


        queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(21);
    }

    public async Task Test11Async()
    {
        bestEstimateIfrsVars = new[]
        {
            new IfrsVariable
            {
                DataNode = "DT1.1", AmountType = "PR", AocType = "CL", Novelty = "I", 
                Partition = testData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(1.0)
            },
            new IfrsVariable
            {
                DataNode = "DT1.1", AmountType = "PR", AocType = "AU", Novelty = "I", 
                Partition = testData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(2.0)
            },
            new IfrsVariable
            {
                DataNode = "DT1.1", AmountType = "PR", AocType = "EV", Novelty = "I", 
                Partition = testData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(3.0)
            },
            new IfrsVariable
            {
                DataNode = "DT1.1", AmountType = "CL", AocType = "CL", Novelty = "I", 
                Partition = testData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(4.0)
            },
            new IfrsVariable
            {
                DataNode = "DT1.1", AmountType = "CL", AocType = "AU", Novelty = "I", 
                Partition = testData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(5.0)
            },
            new IfrsVariable
            {
                DataNode = "DT1.1", AmountType = "CL", AocType = "EV", Novelty = "I", 
                Partition = testData.partition.Id,
                Values = ImportCalculationExtensions.SetProjectionValue(6.0)
            }
        };


    scenarioIfrsVars = new[]
    {
        new IfrsVariable
        {
            DataNode = "DT1.1", AmountType = "PR", AocType = "CL", Novelty = "I", 
            Partition = testData.partitionScenarioMTUP.Id,
            Values = ImportCalculationExtensions.SetProjectionValue(1.1)
        },
        new IfrsVariable
        {
            DataNode = "DT1.1", AmountType = "PR", AocType = "AU", Novelty = "I", 
            Partition = testData.partitionScenarioMTUP.Id,
            Values = ImportCalculationExtensions.SetProjectionValue(2.1)
        }
    };


    var ws = Workspace.CreateNew();
    ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


    await ws.UpdateAsync(bestEstimateIfrsVars);
    await ws.UpdateAsync(scenarioIfrsVars);


    var projectionConfigurations =
        (await ws.Query<ProjectionConfiguration>().ToArrayAsync())
        .SortRelevantProjections(testData.args.Month);


    (await ws.QueryReportVariablesAsync((testData.args.Year, testData.args.Month, 
            testData.args.ReportingNode, testData.args.Scenario),
        projectionConfigurations)).Select(x => x.Value).Sum().Should().Be(21);
}

    public async Task Test12Async()
    {
        var ws = Workspace.CreateNew();
        ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        await ws.UpdateAsync(bestEstimateIfrsVars);
        await ws.UpdateAsync(scenarioIfrsVars);


        var projectionConfigurations =
            (await ws.Query<ProjectionConfiguration>().ToArrayAsync())
            .SortRelevantProjections(testData.args.Month);


        (await ws.QueryReportVariablesAsync((testData.args.Year, testData.args.Month, 
                testData.args.ReportingNode, testData.argsScenarioMTUP.Scenario),
            projectionConfigurations)).Select(x => x.Value).Sum().Should().Be(21.2);
    }

}

