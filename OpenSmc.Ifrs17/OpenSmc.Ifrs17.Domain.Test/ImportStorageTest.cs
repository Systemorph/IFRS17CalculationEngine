using FluentAssertions;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Import;
using Systemorph.Vertex.Scopes.Proxy;
using Systemorph.Vertex.Workspace;

namespace OpenSmc.Ifrs17.Domain.Tests;

public class ImportStorageTest : TestBase
{
    public ImportStorageTest(IImportVariable import, IDataSource dataSource,
        IWorkspaceVariable work, IActivityVariable activity, IScopeFactory scopes) :
        base(import, dataSource, work, activity, scopes)
    {
    }

    public async Task IntializeAsync()
    {
        await TestData.InitializeAsync();
        DataSource.Reset(x => x.ResetCurrentPartitions());
        Work.Reset(x => x.ResetCurrentPartitions());


        await Import.FromString(TestData.novelties)
            .WithType<Novelty>().WithTarget(DataSource).ExecuteAsync();
        await Import.FromString(TestData.canonicalAocTypes)
            .WithType<AocType>().WithTarget(DataSource).ExecuteAsync();

        await Import.FromString(TestData.canonicalAocConfig)
            .WithFormat(ImportFormats.AocConfiguration).WithTarget(DataSource)
            .ExecuteAsync();


        await DataSource.UpdateAsync<Portfolio>(TestData.dt1.RepeatOnce());

        await DataSource.UpdateAsync<Portfolio>(TestData.dtr1.RepeatOnce());

        await DataSource.UpdateAsync<GroupOfInsuranceContract>(TestData.dt11.RepeatOnce());

        await DataSource.UpdateAsync<GroupOfReinsuranceContract>(TestData.dtr11.RepeatOnce());


        await DataSource.UpdateAsync<DataNodeState>(new[]
        {
            TestData.dt11State, TestData.dtr11State
        });

        await DataSource.UpdateAsync(TestData.dt11Inter.RepeatOnce());


        await DataSource.UpdateAsync(new[]
        {
            TestData.yieldCurve, TestData.yieldCurvePrevious
        });


        await Import.FromString(TestData.estimateType)
            .WithType<EstimateType>().WithTarget(DataSource).ExecuteAsync();


        Work.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        await DataSource.UpdateAsync<PartitionByReportingNodeAndPeriod>(new[]
        {
            TestData.partition, TestData.previousPeriodPartition, TestData.partitionScenarioMTUP
        });

        await DataSource.UpdateAsync<PartitionByReportingNode>(TestData.partitionReportingNode.RepeatOnce());
    }

    public async Task StorageInitializeAsync<T>(ImportStorage storage, IEnumerable<T> inputForWorkspace,
        IEnumerable<T> inputForDataSource, ImportArgs args)
    {
        //Prepare Workspace and DataSource
        await Work.UpdateAsync<T>(inputForWorkspace);
        await DataSource.UpdateAsync<T>(inputForDataSource);
        //Set up import storage and test universe
        await storage.InitializeAsync();
        //Clean up 
        await Work.DeleteAsync<T>(inputForWorkspace);
        await DataSource.DeleteAsync<T>(inputForDataSource);
    }


    public async Task<ActivityLog> CheckIfrsVariableImportStorageAsync(IfrsVariable[] inputForWorkspace,
        IfrsVariable[] inputForDataSource, IfrsVariable[] benchmark, ImportArgs args, IActivityVariable activity)
    {
        activity.Start();
        var storage = new ImportStorage(args, DataSource, Work);
        await StorageInitializeAsync(storage, inputForWorkspace, inputForDataSource, args);
        var variables = storage.IfrsVariablesByImportIdentity.SelectMany(x => x.Value);
        var errors = new List<string>();

        var ivc = IfrsVariableComparer
            .Instance(); // This equality comparer does not take partition into account. For this test partition should also be included.

        var extraVariablesInStorage = variables.Except(benchmark, ivc)
            .Select(x => x.ToIdentityString() + " Value: " + String.Join(",", x.Values)).ToArray();
        if (extraVariablesInStorage.Any())
            errors.Add(
                $"IfrsVariables in the storage contain the following items that are not present in the benchmark:\n{string.Join("\n", extraVariablesInStorage)}.");

        var extraVariablesInBenchmark = benchmark.Except(variables, ivc)
            .Select(x => x.ToIdentityString() + " Value: " + String.Join(",", x.Values)).ToArray();
        if (extraVariablesInBenchmark.Any())
            errors.Add(
                $"IfrsVariables in the benchmark contain the following items that are not present in the storage:\n{string.Join("\n", extraVariablesInBenchmark)}.");

        var ivsByIdentityString =
            variables.GroupBy(x => x.ToIdentityString()).Where(x => x.Count() > 1).Select(x => x.Key);
        if (ivsByIdentityString.Any())
            errors.Add(
                $"IfrsVariables in the storage have duplicated items for:\n{string.Join("\n", ivsByIdentityString)}.");

        if (errors.Any()) ApplicationMessage.Log(Error.Generic, string.Join("\n", errors));
        return activity.Finish();
    }


    public async Task<ActivityLog> CheckRawVariableImportStorageAsync(RawVariable[] inputForWorkspace,
        RawVariable[] inputForDataSource, RawVariable[] benchmark, ImportArgs args,
        IActivityVariable activity)
    {
        activity.Start();
        var storage = new ImportStorage(args, DataSource, Work);
        await StorageInitializeAsync(storage, inputForWorkspace, inputForDataSource, args);
        var variables = storage.RawVariablesByImportIdentity.SelectMany(x => x.Value);
        var errors = new List<string>();

        var extraVariablesInStorage = variables.Except(benchmark, RawVariableComparer.Instance())
            .Select(x => x.ToIdentityString() + " Values: " + String.Join(",", x.Values)).ToArray();
        if (extraVariablesInStorage.Any())
            errors.Add(
                $"RawVariables in the storage contain the following items that are not present in the benchmark:\n{string.Join("\n", extraVariablesInStorage)}.");

        var extraVariablesInBenchmark = benchmark.Except(variables, RawVariableComparer.Instance())
            .Select(x => x.ToIdentityString() + " Values: " + String.Join(",", x.Values)).ToArray();
        if (extraVariablesInBenchmark.Any())
            errors.Add(
                $"RawVariables in the benchmark contain the following items that are not present in the storage:\n{string.Join("\n", extraVariablesInBenchmark)}.");

        var ivsByIdentityString =
            variables.GroupBy(x => x.ToIdentityString()).Where(x => x.Count() > 1).Select(x => x.Key);
        if (ivsByIdentityString.Any())
            errors.Add(
                $"RawVariables in the storage have duplicated items for:\n{string.Join("\n", ivsByIdentityString)}.");

        if (errors.Any()) ApplicationMessage.Log(Error.Generic, string.Join("\n", errors));
        return activity.Finish();
    }

    [Fact]
    public async Task Test1()
    {
        var basicIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id, DataNode = TestData.groupOfInsuranceContracts,
            AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, EstimateType = EstimateTypes.AA
        };

        var inputForDataSource = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP, Values = new double[] {100.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP,
                EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {-10.0}},
            basicIfrsVariable with {AocType = AocTypes.WO, Values = new double[] {15.0}},
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
            basicIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {100.0}},
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {-10.0}},
            basicIfrsVariable with {AocType = AocTypes.WO, Values = new double[] {15.0}},
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.args with {ImportFormat = ImportFormats.Actual}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test2()
    {
        var basicIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id, DataNode = TestData.groupOfInsuranceContracts,
            AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.I, EstimateType = EstimateTypes.BE
        };

        var inputForDataSource = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id,
                AocType = AocTypes.EOP, EstimateType = EstimateTypes.AA,
                Novelty = Novelties.C, Values = new double[] {100.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id,
                AocType = AocTypes.EOP, Values = new double[] {1000.0}
            },
            basicIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {1000.0}},
            basicIfrsVariable with {AocType = AocTypes.IA, Values = new double[] {1500.0}},
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {2500.0}},
            basicIfrsVariable with {AocType = AocTypes.EOP, Values = new double[] {5000.0}},
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                AocType = AocTypes.CF, EstimateType = EstimateTypes.AA, Novelty = Novelties.C,
                Values = new double[] {-15.0}
            },
            basicIfrsVariable with
            {
                AocType = AocTypes.WO, EstimateType = EstimateTypes.AA, Novelty = Novelties.C,
                Values = new double[] {-20.0}
            },
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {1000.0}},
            basicIfrsVariable with {AocType = AocTypes.IA, Values = new double[] {1500.0}},
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {2500.0}},
            basicIfrsVariable with {AocType = AocTypes.EOP, Values = new double[] {5000.0}},
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, EstimateType = EstimateTypes.AA,
                Values = new double[] {100.0}
            },
            basicIfrsVariable with
            {
                AocType = AocTypes.CF, EstimateType = EstimateTypes.AA, Novelty = Novelties.C,
                Values = new double[] {-15.0}
            },
            basicIfrsVariable with
            {
                AocType = AocTypes.WO, EstimateType = EstimateTypes.AA, Novelty = Novelties.C,
                Values = new double[] {-20.0}
            },
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.args with {ImportFormat = ImportFormats.Actual}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test3()
    {
        var basicIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id, DataNode = TestData.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, EstimateType = EstimateTypes.AA
        };

        var inputForDataSource = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP, Values = new double[] {100.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP,
                EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
            basicIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {100.0}},
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {150.0}},
            basicIfrsVariable with {AocType = AocTypes.WO, Values = new double[] {200.0}},
            basicIfrsVariable with {AocType = AocTypes.EOP, Values = new double[] {450.0}},
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {-15.0}},
            basicIfrsVariable with {AocType = AocTypes.WO, Values = new double[] {-20.0}},
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
            basicIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {100.0}},
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {-15.0}},
            basicIfrsVariable with {AocType = AocTypes.WO, Values = new double[] {-20.0}},
            basicIfrsVariable with {AocType = AocTypes.EOP, Values = new double[] {450.0}},
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.args with {ImportFormat = ImportFormats.Actual}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test4()
    {
        var basicAdvanceActualIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id, DataNode = TestData.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, EstimateType = EstimateTypes.AA
        };

        var basicBeIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id, DataNode = TestData.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.I, EstimateType = EstimateTypes.BE
        };

        var inputForDataSource = new IfrsVariable[]
        {
            basicBeIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP, Values = new double[] {1000.0}
            },
            basicBeIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {1000.0}},
            basicBeIfrsVariable with {AocType = AocTypes.IA, Values = new double[] {1500.0}},
            basicBeIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {2500.0}},
            basicBeIfrsVariable with {AocType = AocTypes.EOP, Values = new double[] {5000.0}},
            basicAdvanceActualIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP, Values = new double[] {100.0}
            },
            basicAdvanceActualIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP,
                EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
            basicAdvanceActualIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
            basicAdvanceActualIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {100.0}
            },
            basicAdvanceActualIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {150.0}},
            basicAdvanceActualIfrsVariable with {AocType = AocTypes.WO, Values = new double[] {200.0}},
            basicAdvanceActualIfrsVariable with {AocType = AocTypes.EOP, Values = new double[] {450.0}},
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicAdvanceActualIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {-15.0}},
            basicAdvanceActualIfrsVariable with {AocType = AocTypes.WO, Values = new double[] {-20.0}},
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            basicBeIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {1000.0}},
            basicBeIfrsVariable with {AocType = AocTypes.IA, Values = new double[] {1500.0}},
            basicBeIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {2500.0}},
            basicBeIfrsVariable with {AocType = AocTypes.EOP, Values = new double[] {5000.0}},
            basicAdvanceActualIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
            basicAdvanceActualIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {100.0}
            },
            basicAdvanceActualIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {-15.0}},
            basicAdvanceActualIfrsVariable with {AocType = AocTypes.WO, Values = new double[] {-20.0}},
            basicAdvanceActualIfrsVariable with {AocType = AocTypes.EOP, Values = new double[] {450.0}},
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.args with {ImportFormat = ImportFormats.Actual}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test5()
    {
        var basicIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id, DataNode = TestData.groupOfReinsuranceContracts, AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, EstimateType = EstimateTypes.AA
        };

        var inputForDataSource = new IfrsVariable[]
        {
            //Year
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP, Values = new double[] {100.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP,
                EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP,
                DataNode = TestData.groupOfInsuranceContracts,
                EstimateType = EstimateTypes.DA, Values = new double[] {1000.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP,
                DataNode = TestData.groupOfInsuranceContracts,
                EstimateType = EstimateTypes.BE, EconomicBasis = EconomicBases.C, Values = new double[] {1000.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.CL,
                DataNode = TestData.groupOfInsuranceContracts,
                EstimateType = EstimateTypes.BE, EconomicBasis = EconomicBases.C, Values = new double[] {666.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP,
                DataNode = TestData.groupOfInsuranceContracts,
                EstimateType = EstimateTypes.BE, EconomicBasis = EconomicBases.L, Values = new double[] {1000.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.CL,
                DataNode = TestData.groupOfInsuranceContracts,
                EstimateType = EstimateTypes.BE, EconomicBasis = EconomicBases.L, Values = new double[] {666.0}
            },
            //Year -1
            basicIfrsVariable with
            {
                AocType = AocTypes.IA, Novelty = Novelties.I, DataNode = TestData.groupOfInsuranceContracts,
                EstimateType = EstimateTypes.BE, EconomicBasis = EconomicBases.L, Values = new double[] {10.0}
            },
            basicIfrsVariable with
            {
                AocType = AocTypes.CF, Novelty = Novelties.I, DataNode = TestData.groupOfInsuranceContracts,
                EstimateType = EstimateTypes.BE, EconomicBasis = EconomicBases.L, Values = new double[] {10.0}
            },
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {-10.0}},
            basicIfrsVariable with {AocType = AocTypes.WO, Values = new double[] {15.0}},
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            //From previous Period
            //Actuals
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
            basicIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {100.0}},
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, DataNode = TestData.groupOfInsuranceContracts,
                EstimateType = EstimateTypes.DA, Values = new double[] {1000.0}
            },
            //Cash flow
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, DataNode = TestData.groupOfInsuranceContracts,
                EstimateType = EstimateTypes.BE, EconomicBasis = EconomicBases.L, Values = new double[] {1000.0}
            },
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, DataNode = TestData.groupOfInsuranceContracts,
                EstimateType = EstimateTypes.BE, EconomicBasis = EconomicBases.C, Values = new double[] {1000.0}
            },

            //From current Period
            //from DB
            basicIfrsVariable with
            {
                AocType = AocTypes.IA, Novelty = Novelties.I, DataNode = TestData.groupOfInsuranceContracts,
                EstimateType = EstimateTypes.BE, EconomicBasis = EconomicBases.L, Values = new double[] {10.0}
            },
            basicIfrsVariable with
            {
                AocType = AocTypes.CF, Novelty = Novelties.I, DataNode = TestData.groupOfInsuranceContracts,
                EstimateType = EstimateTypes.BE, EconomicBasis = EconomicBases.L, Values = new double[] {10.0}
            },

            //from workspace
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {-10.0}},
            basicIfrsVariable with {AocType = AocTypes.WO, Values = new double[] {15.0}},
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.args with {ImportFormat = ImportFormats.Actual}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test6()
    {
        await Work.DeleteAsync(await Work.Query<DataNodeState>().ToArrayAsync());
        await DataSource.DeleteAsync(await DataSource.Query<DataNodeState>().ToArrayAsync());
        await Work.UpdateAsync(new[]
        {
            TestData.dt11State with {Year = TestData.args.Year, Month = TestData.args.Month},
            TestData.dtr11State with {Year = TestData.args.Year, Month = TestData.args.Month}
        });

        await DataSource.UpdateAsync(new[]
        {
            TestData.dt11State with {Year = TestData.args.Year, Month = TestData.args.Month},
            TestData.dtr11State with {Year = TestData.args.Year, Month = TestData.args.Month}
        });


        var basicIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id, DataNode = TestData.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, EstimateType = EstimateTypes.A
        };

        var inputForDataSource = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {90.0}},
            basicIfrsVariable with
            {
                DataNode = TestData.groupOfReinsuranceContracts,
                AocType = AocTypes.BOP, Novelty = Novelties.I,
                Values = new double[] {89.0}
            },
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {91.0}},
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {91.0}},
            basicIfrsVariable with
            {
                DataNode = TestData.groupOfReinsuranceContracts,
                AocType = AocTypes.BOP, Novelty = Novelties.I,
                Values = new double[] {89.0}
            },
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.args with {ImportFormat = ImportFormats.Opening}, Activity);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }


    [Fact]
    public async Task Test7()
    {
        await Work.DeleteAsync(await Work.Query<DataNodeState>().ToArrayAsync());
        await DataSource.DeleteAsync(await DataSource.Query<DataNodeState>().ToArrayAsync());
        await DataSource.UpdateAsync(new[]
        {
            TestData.dt11State, TestData.dtr11State
        });

        await Work.UpdateAsync(new[]
        {
            TestData.dt11State, TestData.dtr11State
        });


        await Work.DeleteAsync(await Work.Query<DataNodeState>().ToArrayAsync());
        await DataSource.DeleteAsync(await DataSource.Query<DataNodeState>().ToArrayAsync());
        await Work.UpdateAsync(new[]
        {
            TestData.dt11State with {Year = TestData.args.Year, Month = TestData.args.Month},
            TestData.dtr11State with {Year = TestData.args.Year, Month = TestData.args.Month}
        });

        await DataSource.UpdateAsync(new[]
        {
            TestData.dt11State with {Year = TestData.args.Year, Month = TestData.args.Month},
            TestData.dtr11State with {Year = TestData.args.Year, Month = TestData.args.Month}
        });


        var basicIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id, DataNode = TestData.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, EstimateType = EstimateTypes.A
        };

        var inputForDataSource = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {90.0}},
            basicIfrsVariable with
            {
                DataNode = TestData.groupOfReinsuranceContracts,
                AocType = AocTypes.BOP, Novelty = Novelties.I,
                Values = new double[] {89.0}
            },
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                DataNode = TestData.groupOfReinsuranceContracts,
                AocType = AocTypes.BOP, Novelty = Novelties.I,
                Values = new double[] {91.0}
            },
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {90.0}},
            basicIfrsVariable with
            {
                DataNode = TestData.groupOfReinsuranceContracts,
                AocType = AocTypes.BOP, Novelty = Novelties.I,
                Values = new double[] {91.0},
            }
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.args with {ImportFormat = ImportFormats.Opening}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }


    [Fact]
    public async Task Test8()
    {
        await Work.DeleteAsync(await Work.Query<DataNodeState>().ToArrayAsync());
        await DataSource.DeleteAsync(await DataSource.Query<DataNodeState>().ToArrayAsync());
        await DataSource.UpdateAsync(new[]
        {
            TestData.dt11State, TestData.dtr11State
        });

        await Work.UpdateAsync(new[]
        {
            TestData.dt11State, TestData.dtr11State
        });


        var basicIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id, DataNode = TestData.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, EstimateType = EstimateTypes.A
        };

        var inputForDataSource = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, EstimateType = EstimateTypes.DA, Values = new double[] {1000.0}
            },
            basicIfrsVariable with {AocType = AocTypes.BOP, Values = new double[] {100.0}},
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {150.0}},
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.CF, Values = new double[] {-15.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AmountType = AmountTypes.CL, AocType = AocTypes.CF,
                Values = new double[] {-99.0}
            },
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.partition.Id, AocType = AocTypes.BOP, EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partition.Id,
                AocType = AocTypes.BOP, Values = new double[] {100.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.CF, Values = new double[] {-15.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AmountType = AmountTypes.CL, AocType = AocTypes.CF,
                Values = new double[] {-99.0}
            },
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.argsScenarioMTUP with {ImportFormat = ImportFormats.Actual}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test9()
    {
        var basicIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id, DataNode = TestData.groupOfInsuranceContracts,
            AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, EstimateType = EstimateTypes.A
        };

        var inputForDataSource = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, EstimateType = EstimateTypes.DA, Values = new double[] {1000.0}
            },
            basicIfrsVariable with {AocType = AocTypes.BOP, Values = new double[] {100.0}},
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {150.0}},
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                DataNode = TestData.groupOfReinsuranceContracts, AocType = AocTypes.CF,
                Values = new double[] {-15.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                DataNode = TestData.groupOfReinsuranceContracts,
                AmountType = AmountTypes.CL, AocType = AocTypes.CF, Values = new double[] {-99.0}
            },
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.partition.Id, AocType = AocTypes.BOP, EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partition.Id,
                AocType = AocTypes.BOP, Values = new double[] {100.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partition.Id,
                AocType = AocTypes.CF, Values = new double[] {150.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                DataNode = TestData.groupOfReinsuranceContracts, AocType = AocTypes.CF,
                Values = new double[] {-15.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                DataNode = TestData.groupOfReinsuranceContracts,
                AmountType = AmountTypes.CL, AocType = AocTypes.CF, Values = new double[] {-99.0}
            },
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.argsScenarioMTUP with {ImportFormat = ImportFormats.Actual}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test10()
    {
        var basicIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id,
            DataNode = TestData.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, EstimateType = EstimateTypes.A
        };

        var inputForDataSource = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, EstimateType = EstimateTypes.DA, Values = new double[] {1000.0}
            },
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {150.0}},
            basicIfrsVariable with {AocType = AocTypes.BOP, Novelty = Novelties.I, Values = new double[] {90.0}},
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.BOP, Novelty = Novelties.I,
                Values = new double[] {90.7}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP, Novelty = Novelties.C,
                Values = new double[] {89.5}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartitionScenarioMTUP.Id, AocType = AocTypes.EOP,
                Novelty = Novelties.C,
                Values = new double[] {89.1}
            }
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                AocType = AocTypes.CF, Values = new double[] {-15.0}
            },
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.partition.Id, AocType = AocTypes.BOP, EstimateType = EstimateTypes.DA,
                Values = new double[] {1000.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.BOP, Novelty = Novelties.I,
                Values = new double[] {89.5}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.CF, Values = new double[] {-15.0}
            },
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.argsScenarioMTUP with {ImportFormat = ImportFormats.Actual}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test11()
    {
        await Work.DeleteAsync(await Work.Query<DataNodeState>().ToArrayAsync());
        await DataSource.DeleteAsync(await DataSource.Query<DataNodeState>().ToArrayAsync());
        await Work.UpdateAsync(new[]
        {
            TestData.dt11State with {Year = TestData.args.Year, Month = TestData.args.Month},
            TestData.dtr11State with {Year = TestData.args.Year, Month = TestData.args.Month}
        });

        await DataSource.UpdateAsync(new[]
        {
            TestData.dt11State with {Year = TestData.args.Year, Month = TestData.args.Month},
            TestData.dtr11State with {Year = TestData.args.Year, Month = TestData.args.Month}
        });


        var basicIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id, DataNode = TestData.groupOfInsuranceContracts,
            AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, EstimateType = EstimateTypes.A
        };

        var inputForDataSource = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {150.0}},
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, EstimateType = EstimateTypes.AA,
                Values = new double[] {90.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.BOP, Novelty = Novelties.I,
                EstimateType = EstimateTypes.AA, Values = new double[] {90.7}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id, AocType = AocTypes.EOP, Novelty = Novelties.C,
                EstimateType = EstimateTypes.AA, Values = new double[] {89.5}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartitionScenarioMTUP.Id, AocType = AocTypes.EOP,
                Novelty = Novelties.C,
                EstimateType = EstimateTypes.AA, Values = new double[] {89.1}
            }
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.CF, Values = new double[] {-15.0}
            },
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.BOP, Novelty = Novelties.I,
                EstimateType = EstimateTypes.AA, Values = new double[] {90.7}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.CF, Values = new double[] {-15.0}
            },
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.argsScenarioMTUP with {ImportFormat = ImportFormats.Actual}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test12()
    {
        await Work.DeleteAsync(await Work.Query<DataNodeState>().ToArrayAsync());
        await DataSource.DeleteAsync(await DataSource.Query<DataNodeState>().ToArrayAsync());
        await Work.UpdateAsync(new[]
        {
            TestData.dt11State with {Year = TestData.args.Year, Month = TestData.args.Month},
            TestData.dtr11State with {Year = TestData.args.Year, Month = TestData.args.Month}
        });

        await DataSource.UpdateAsync(new[]
        {
            TestData.dt11State with {Year = TestData.args.Year, Month = TestData.args.Month},
            TestData.dtr11State with {Year = TestData.args.Year, Month = TestData.args.Month}
        });


        var basicIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id,
            DataNode = TestData.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, EstimateType = EstimateTypes.A
        };

        var inputForDataSource = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {150.0}},
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, EstimateType = EstimateTypes.AA,
                Values = new double[] {90.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id,
                AocType = AocTypes.EOP, Novelty = Novelties.C,
                EstimateType = EstimateTypes.AA, Values = new double[] {89.5}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartitionScenarioMTUP.Id,
                AocType = AocTypes.EOP, Novelty = Novelties.C,
                EstimateType = EstimateTypes.AA, Values = new double[] {89.1}
            }
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.CF, Values = new double[] {-15.0}
            },
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                AocType = AocTypes.BOP, Novelty = Novelties.I, EstimateType = EstimateTypes.AA,
                Values = new double[] {90.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.CF, Values = new double[] {-15.0}
            },
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.argsScenarioMTUP with {ImportFormat = ImportFormats.Actual}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test13()
    {
        await Work.DeleteAsync(await Work.Query<DataNodeState>().ToArrayAsync());
        await DataSource.DeleteAsync(await DataSource.Query<DataNodeState>().ToArrayAsync());
        await Work.UpdateAsync(new[]
        {
            TestData.dt11State, TestData.dtr11State with {Year = TestData.args.Year, Month = TestData.args.Month}
        });

        await DataSource.UpdateAsync(new[]
        {
            TestData.dt11State, TestData.dtr11State with
            {
                Year = TestData.args.Year,
                Month = TestData.args.Month
            }
        });


        var basicIfrsVariable = new IfrsVariable
        {
            Partition = TestData.partition.Id,
            DataNode = TestData.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, EstimateType = EstimateTypes.A
        };

        var inputForDataSource = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = AocTypes.CF, Values = new double[] {150.0}},
            basicIfrsVariable with
            {
                DataNode = TestData.groupOfReinsuranceContracts, AocType = AocTypes.BOP, Novelty = Novelties.I,
                Values = new double[] {90.0}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                DataNode = TestData.groupOfReinsuranceContracts, AocType = AocTypes.BOP,
                Novelty = Novelties.I, Values = new double[] {90.7}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartition.Id,
                DataNode = TestData.groupOfInsuranceContracts, AocType = AocTypes.EOP,
                Novelty = Novelties.C, Values = new double[] {89.5}
            },
            basicIfrsVariable with
            {
                Partition = TestData.previousPeriodPartitionScenarioMTUP.Id,
                DataNode = TestData.groupOfInsuranceContracts,
                AocType = AocTypes.EOP, Novelty = Novelties.C, Values = new double[] {89.1}
            }
        };

        var inputForWorkspace = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.CF, Values = new double[] {-15.0}
            },
        };

        var ivsBenchmark = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.BOP, Novelty = Novelties.I,
                Values = new double[] {89.5}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                DataNode = TestData.groupOfReinsuranceContracts, AocType = AocTypes.BOP,
                Novelty = Novelties.I, Values = new double[] {90.7}
            },
            basicIfrsVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id, AocType = AocTypes.CF, Values = new double[] {-15.0}
            },
        };

        var activity = await CheckIfrsVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.argsScenarioMTUP with {ImportFormat = ImportFormats.Actual}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }


    [Fact]
    public async Task Test14()
    {
        await Work.DeleteAsync(await Work.Query<DataNodeState>().ToArrayAsync());
        await DataSource.DeleteAsync(await DataSource.Query<DataNodeState>().ToArrayAsync());
        await DataSource.UpdateAsync(new[]
        {
            TestData.dt11State, TestData.dtr11State
        });

        await Work.UpdateAsync(new[]
        {
            TestData.dt11State, TestData.dtr11State
        });


        var basicRawVariable = new RawVariable
        {
            Partition = TestData.partition.Id,
            DataNode = TestData.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, AocType = AocTypes.CL, EstimateType = EstimateTypes.BE
        };

        var inputForDataSource = new RawVariable[]
        {
            basicRawVariable with {Values = new[] {150.0}},
            basicRawVariable with {AmountType = AmountTypes.CL, Values = new[] {99.0}},
            basicRawVariable with
            {
                Partition = TestData.partitionScenarioMTDOWN.Id,
                AocType = AocTypes.CL, Novelty = Novelties.C,
                Values = new[] {130.0}
            }
        };

        var inputForWorkspace = new RawVariable[]
        {
            basicRawVariable with {Partition = TestData.partitionScenarioMTUP.Id, Values = new[] {110.0}},
        };

        var ivsBenchmark = new RawVariable[]
        {
            basicRawVariable with {AmountType = AmountTypes.CL, Values = new[] {99.0}},
            basicRawVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                Values = new[] {110.0}
            },
        };

        var activity = await CheckRawVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.argsScenarioMTUP with {ImportFormat = ImportFormats.Cashflow}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test15()
    {
        var basicRawVariable = new RawVariable
        {
            Partition = TestData.partition.Id,
            DataNode = TestData.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = AmountTypes.PR, Novelty = Novelties.C, AocType = AocTypes.CL, EstimateType = EstimateTypes.BE
        };

        var inputForDataSource = new RawVariable[]
        {
            basicRawVariable with {Values = new[] {150.0}},
            basicRawVariable with {AmountType = AmountTypes.CL, Values = new[] {99.0}},
            basicRawVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                AmountType = AmountTypes.CL, Values = new[] {130.0}
            },
            basicRawVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                DataNode = TestData.groupOfReinsuranceContracts,
                AmountType = AmountTypes.CL, Values = new[] {125.0}
            }
        };

        var inputForWorkspace = new RawVariable[]
        {
            basicRawVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                Values = new[] {110.0}
            },
        };

        var ivsBenchmark = new RawVariable[]
        {
            basicRawVariable with {AmountType = AmountTypes.CL, Values = new[] {99.0}},
            basicRawVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                DataNode = TestData.groupOfReinsuranceContracts,
                AmountType = AmountTypes.CL, Values = new[] {125.0}
            },
            basicRawVariable with
            {
                Partition = TestData.partitionScenarioMTUP.Id,
                Values = new[] {110.0}
            },
        };

        var activity = await CheckRawVariableImportStorageAsync(inputForWorkspace, inputForDataSource, ivsBenchmark,
            TestData.argsScenarioMTUP with {ImportFormat = ImportFormats.Cashflow}, Activity);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }
}

public class ImportStorageTestPart2 : ImportStorageTest
{
    private readonly ImportArgs args;
    private readonly GroupOfInsuranceContract[] inputDataGic;
    private readonly GroupOfReinsuranceContract[] inputDataGric;
    private readonly DataNodeState[] inputDataState;
    private readonly InterDataNodeParameter[] inputDataParameter;
    private readonly RawVariable sampleRawVar;

    public ImportStorageTestPart2(IImportVariable import, IDataSource dataSource,
        IWorkspaceVariable work, IActivityVariable activity, IScopeFactory scopes) :
        base(import, dataSource, work, activity, scopes)
    {
        args = new ImportArgs("CH", 2021, 3, Periodicity.Quarterly, null, ImportFormats.Cashflow);

        var reportingNodePartition = DataSource.Query<PartitionByReportingNode>()
            .FirstOrDefault(x => x.ReportingNode == args.ReportingNode);
        if (reportingNodePartition == null) ApplicationMessage.Log(Error.PartitionNotFound);


        var currentPartition = DataSource.Query<PartitionByReportingNodeAndPeriod>()
            .FirstOrDefault(x =>
                x.ReportingNode == args.ReportingNode && x.Year == args.Year &&
                x.Month == args.Month && x.Scenario == args.Scenario);
        if (currentPartition == null) ApplicationMessage.Log(Error.PartitionNotFound);


        var previousPeriodPartition = Work.Query<PartitionByReportingNodeAndPeriod>().FirstOrDefault(x =>
            x.ReportingNode == args.ReportingNode && x.Year == args.Year - 1 &&
            x.Month == Consts.MonthInAYear && x.Scenario == args.Scenario);
        if (previousPeriodPartition == null) ApplicationMessage.Log(Error.PartitionNotFound);


        var sampleGic = new GroupOfInsuranceContract() {Portfolio = "P1"};

        inputDataGic = new GroupOfInsuranceContract[]
        {
            sampleGic with
            {
                SystemName = "Gross1", LiabilityType = LiabilityTypes.LRC, ContractualCurrency = "USD",
                ValuationApproach = "BBA"
            },
            sampleGic with
            {
                SystemName = "Gross2", LiabilityType = LiabilityTypes.LRC, ContractualCurrency = "USD",
                ValuationApproach = "BBA"
            },
            sampleGic with
            {
                SystemName = "Gross3", LiabilityType = LiabilityTypes.LRC, ContractualCurrency = "USD",
                ValuationApproach = "BBA"
            },
        };

        var sampleGric = new GroupOfReinsuranceContract() {Portfolio = "ReP1"};

        inputDataGric = new GroupOfReinsuranceContract[]
        {
            sampleGric with
            {
                SystemName = "Reins1", LiabilityType = LiabilityTypes.LRC, ContractualCurrency = "USD",
                ValuationApproach = "BBA"
            },
            sampleGric with
            {
                SystemName = "Reins2", LiabilityType = LiabilityTypes.LRC, ContractualCurrency = "USD",
                ValuationApproach = "BBA"
            },
            sampleGric with
            {
                SystemName = "Reins3", LiabilityType = LiabilityTypes.LRC, ContractualCurrency = "USD",
                ValuationApproach = "BBA"
            },
        };

        var sampleDnState = new DataNodeState
            {Partition = reportingNodePartition.Id, Year = args.Year, Month = args.Month, State = State.Active};

        inputDataState = new DataNodeState[]
        {
            sampleDnState with {DataNode = "Gross1"},
            sampleDnState with {DataNode = "Gross2"},
            sampleDnState with {DataNode = "Gross3"},
            sampleDnState with {DataNode = "Reins1"},
            sampleDnState with {DataNode = "Reins2"},
            sampleDnState with {DataNode = "Reins3"},
        };

        var samplePreviousDnParam = new InterDataNodeParameter
            {Partition = reportingNodePartition.Id, ReinsuranceCoverage = 1, Year = args.Year - 1, Month = args.Month};

        var sampleCurrentDnParam = new InterDataNodeParameter
            {Partition = reportingNodePartition.Id, ReinsuranceCoverage = 1, Year = args.Year, Month = args.Month};

        inputDataParameter = new InterDataNodeParameter[]
        {
            samplePreviousDnParam with {DataNode = "Gross3", LinkedDataNode = "Reins3", ReinsuranceCoverage = 1},
            samplePreviousDnParam with {DataNode = "Gross1", LinkedDataNode = "Reins1", ReinsuranceCoverage = 0.1},
            samplePreviousDnParam with {DataNode = "Gross1", LinkedDataNode = "Reins2", ReinsuranceCoverage = 0.2},
            samplePreviousDnParam with {DataNode = "Gross2", LinkedDataNode = "Reins2", ReinsuranceCoverage = 0.3},
            sampleCurrentDnParam with {DataNode = "Gross1", LinkedDataNode = "Reins1", ReinsuranceCoverage = 0.5},
            sampleCurrentDnParam with {DataNode = "Gross1", LinkedDataNode = "Reins2", ReinsuranceCoverage = 0.6},
            sampleCurrentDnParam with {DataNode = "Gross2", LinkedDataNode = "Reins2", ReinsuranceCoverage = 0.7},
            sampleCurrentDnParam with {DataNode = "Gross3", LinkedDataNode = "Reins3", ReinsuranceCoverage = 1.0},
        };

        sampleRawVar = new RawVariable
        {
            AmountType = AmountTypes.PR, AocType = AocTypes.CL, Novelty = Novelties.C, Partition = currentPartition.Id
        };
    }


    public async Task PrepareWorkspaceDataNodes()
    {
        await Work.UpdateAsync<GroupOfInsuranceContract>(inputDataGic);
        await Work.UpdateAsync<GroupOfReinsuranceContract>(inputDataGric);
        await Work.UpdateAsync<DataNodeState>(inputDataState);
        await Work.UpdateAsync<InterDataNodeParameter>(inputDataParameter);
    }


    public async Task CleanWorkspaceDataNodes()
    {
        await Work.DeleteAsync<GroupOfInsuranceContract>(inputDataGic);
        await Work.DeleteAsync<GroupOfReinsuranceContract>(inputDataGric);
        await Work.DeleteAsync<DataNodeState>(inputDataState);
        await Work.DeleteAsync<InterDataNodeParameter>(inputDataParameter);
    }


    public async Task<ActivityLog> CheckGetUnderlyingGicsAsync(RawVariable[] inputDataVariable,
        Dictionary<string, IEnumerable<string>> underlyingGicBm)
    {
        Activity.Start();
        var errors = new List<string>();

        await PrepareWorkspaceDataNodes();
        await Work.UpdateAsync<RawVariable>(inputDataVariable);
        var testStorage = new ImportStorage(args, DataSource, Work);
        await testStorage.InitializeAsync();

        var primaryScopeDn = testStorage.DataNodesByImportScope[ImportScope.Primary];

        foreach (var dn in primaryScopeDn)
        {
            var id = new ImportIdentity() {DataNode = dn};
            if (underlyingGicBm[dn].Except(testStorage.GetUnderlyingGic(id)).Count() != 0)
                errors.Add(
                    $"Underlying Gics for DataNode {dn} not matching with BM. Computed: \n{string.Join("\n", testStorage.GetUnderlyingGic(id))} \n Expected : \n{string.Join("\n", underlyingGicBm[dn])}");
        }

        await Work.DeleteAsync<RawVariable>(await Work.Query<RawVariable>().ToArrayAsync());
        await CleanWorkspaceDataNodes();
        if (errors.Any()) ApplicationMessage.Log(Error.Generic, string.Join("\n", errors));
        return Activity.Finish();
    }

    [Fact]
    public async Task Test16()
    {
        var inputDataVariable = new RawVariable[]
        {
            sampleRawVar with {DataNode = "Reins1"},
            sampleRawVar with {DataNode = "Reins2"},
        };

        var underlyingGicBm = new Dictionary<string, IEnumerable<string>>()
        {
            {"Reins1", new string[] {"Gross1"}},
            {"Reins2", new string[] {"Gross1", "Gross2"}},
        };

        var activity = await CheckGetUnderlyingGicsAsync(inputDataVariable, underlyingGicBm);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test17()
    {
        var inputDataVariable = new RawVariable[]
        {
            sampleRawVar with {DataNode = "Reins2"},
        };

        var underlyingGicBm = new Dictionary<string, IEnumerable<string>>()
        {
            {"Reins2", new string[] {"Gross1", "Gross2"}},
        };

        var activity = await CheckGetUnderlyingGicsAsync(inputDataVariable, underlyingGicBm);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }


    public async Task<ActivityLog> CheckGetReinsuranceCoverageAsync(RawVariable[] inputDataVariable,
        Dictionary<(string, string), double> reinsCovBoPBm,
        Dictionary<(string, string), double> reinsCovEoPBm)
    {
        Activity.Start();
        var errors = new List<string>();

        await PrepareWorkspaceDataNodes();
        await Work.UpdateAsync<RawVariable>(inputDataVariable);
        var testStorage = new ImportStorage(args, DataSource, Work);
        await testStorage.InitializeAsync();

        var primaryScopeDn = testStorage.DataNodesByImportScope[ImportScope.Primary];

        foreach (var dn in primaryScopeDn)
        {
            var aocTypes = new AocStep[]
                {new AocStep(AocTypes.BOP, Novelties.I), new AocStep(AocTypes.RCU, Novelties.I)};
            foreach (var aoc in aocTypes)
            {
                var bm = aoc.AocType == AocTypes.BOP ? reinsCovBoPBm : reinsCovEoPBm;
                var id = new ImportIdentity() {DataNode = dn, AocType = aoc.AocType, Novelty = aoc.Novelty};

                var computedReinsCov = testStorage.GetUnderlyingGic(id)
                    .Select(gic => (g: gic, value: testStorage.GetReinsuranceCoverage(id, gic)))
                    .ToDictionary(x => (dn, x.g), x => x.value);

                if (bm.Keys.Where(x => x.Item1 == dn).Except(computedReinsCov.Keys).Count() != 0)
                    errors.Add(
                        $"Gric-Gic links not matching with BM for DataNode {dn} and AocType {aoc.AocType}. \n Computed: \n{string.Join("\n", computedReinsCov.Keys)} \n Expected: \n{string.Join("\n", bm.Keys)}");

                foreach (var reinsCov in computedReinsCov)
                {
                    var bmKvp = bm.Single(x => x.Key.Item1 == reinsCov.Key.Item1 && x.Key.Item2 == reinsCov.Key.Item2);
                    if (Math.Abs(bmKvp.Value - reinsCov.Value) > Consts.Precision)
                        errors.Add(
                            $"{dn}-{reinsCov.Key.Item2} Reinsurance Coverage not matching with BM for AocType {aoc.AocType}: \n Computed: {reinsCov.Value} \n Expected: {bmKvp.Value}");
                }
            }
        }

        await Work.DeleteAsync<RawVariable>(await Work.Query<RawVariable>().ToArrayAsync());
        await CleanWorkspaceDataNodes();
        if (errors.Any()) ApplicationMessage.Log(Error.Generic, string.Join("\n", errors));
        return Activity.Finish();
    }

    [Fact]
    public async Task Test18()
    {
        var inputDataVariable = new RawVariable[]
        {
            sampleRawVar with {DataNode = "Reins1"},
            sampleRawVar with {DataNode = "Reins2"},
        };

        var reinsCovBoPBm = new Dictionary<(string, string), double>()
        {
            {("Reins1", "Gross1"), 0.1},
            {("Reins2", "Gross1"), 0.2},
            {("Reins2", "Gross2"), 0.3},
        };

        var reinsCovEoPBm = new Dictionary<(string, string), double>()
        {
            {("Reins1", "Gross1"), 0.5},
            {("Reins2", "Gross1"), 0.6},
            {("Reins2", "Gross2"), 0.7},
        };

        var activity = await CheckGetReinsuranceCoverageAsync(inputDataVariable, reinsCovBoPBm, reinsCovEoPBm);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task<ActivityLog> CheckSecondaryScopeAsync(RawVariable[] inputDataVariable,
        string[] primaryScopeBm,
        string[] secondaryScopeBm)
    {
        Activity.Start();
        var errors = new List<string>();

        await PrepareWorkspaceDataNodes();
        await Work.UpdateAsync<RawVariable>(inputDataVariable);
        var testStorage = new ImportStorage(args, DataSource, Work);
        await testStorage.InitializeAsync();

        var activeDn = (await Work.Query<DataNodeState>().ToArrayAsync()).Select(x => x.DataNode);

        var primaryScopeDn = testStorage.DataNodesByImportScope[ImportScope.Primary];

        foreach (var dn in activeDn)
        {
            //PrimaryScope
            if (primaryScopeBm.Contains(dn) && !primaryScopeDn.Contains(dn))
                errors.Add($"DataNode {dn} is not added to the primary scope.");
            if (!primaryScopeBm.Contains(dn) && primaryScopeDn.Contains(dn))
                errors.Add($"DataNode {dn} is added to the primary scope but should have not.");

            //SecondaryScope
            if (secondaryScopeBm.Contains(dn) && !testStorage.IsSecondaryScope(dn))
                errors.Add($"DataNode {dn} is not added to the secondary scope.");
            if (!secondaryScopeBm.Contains(dn) && testStorage.IsSecondaryScope(dn))
                errors.Add($"DataNode {dn} is added to the secondary scope but should have not.");
        }

        await Work.DeleteAsync<RawVariable>(await Work.Query<RawVariable>().ToArrayAsync());
        await CleanWorkspaceDataNodes();
        if (errors.Any()) ApplicationMessage.Log(Error.Generic, string.Join("\n", errors));
        return Activity.Finish();
    }

    [Fact]
    public async Task Test19()
    {
        var inputDataVariable = new RawVariable[]
        {
            sampleRawVar with {DataNode = "Reins1"},
            sampleRawVar with {DataNode = "Reins2"},
        };

        var primaryScopeBm = new string[] {"Reins1", "Reins2"};
        var secondaryScopeBm = new string[] {"Gross1", "Gross2"};
        var activity = await CheckSecondaryScopeAsync(inputDataVariable, primaryScopeBm, secondaryScopeBm);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test20()
    {
        var inputDataVariable = new RawVariable[]
        {
            sampleRawVar with {DataNode = "Reins1"},
        };

        var primaryScopeBm = new string[] {"Reins1"};
        var secondaryScopeBm = new string[] {"Gross1"};
        var activity = await CheckSecondaryScopeAsync(inputDataVariable, primaryScopeBm, secondaryScopeBm);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test21()
    {
        var inputDataVariable = new RawVariable[]
        {
            sampleRawVar with {DataNode = "Reins1"},
            sampleRawVar with {DataNode = "Gross1"},
        };

        var primaryScopeBm = new string[] {"Reins1", "Gross1", "Reins2"};
        var secondaryScopeBm = new string[] {"Gross2"};
        var activity = await CheckSecondaryScopeAsync(inputDataVariable, primaryScopeBm, secondaryScopeBm);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }
}