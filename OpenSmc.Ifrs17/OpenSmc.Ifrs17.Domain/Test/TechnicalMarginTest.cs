using FluentAssertions;
using Microsoft.Graph.SecurityNamespace;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Export.Factory;
using Systemorph.Vertex.Import;
using Systemorph.Vertex.Pivot.Builder.Interfaces;
using Systemorph.Vertex.Scopes.Proxy;
using Systemorph.Vertex.Workspace;

//#!import "../Import/Importers"
//#!import "TestData"

public class TechnicalMarginTest
{
    protected IWorkspaceVariable Workspace;
    protected TestData td;
    protected IActivityVariable Activity;
    protected IDataSource DataSource;
    protected IImportVariable Import;
    protected IPivotFactory Report;
    protected IExportVariable Export;
    protected IScopeFactory Scopes;

    public TechnicalMarginTest(IWorkspaceVariable workspace, IActivityVariable activity,
        IDataSource dataSource, IImportVariable import)
    {
        Workspace = workspace;
        Activity = activity;
        DataSource = dataSource;
        Import = import;
        td = new TestData();
    }

    public async Task InitializeAsync()
    {
        td.InitializeAsync();
        await Import.FromString(td.novelties).WithType<Novelty>().WithTarget(DataSource).ExecuteAsync();
        await Import.FromString(td.canonicalAocTypes).WithType<AocType>().WithTarget(DataSource).ExecuteAsync();

        await Import.FromString(td.canonicalAocConfig).WithFormat(ImportFormats.AocConfiguration).WithTarget(DataSource)
            .ExecuteAsync();


        await DataSource.UpdateAsync<Portfolio>(new[]
        {
            td.dt1, td.dtr1
        });

        await DataSource.UpdateAsync<GroupOfContract>(new[]
        {
            td.dt11
        });

        await DataSource.UpdateAsync<GroupOfReinsuranceContract>(new[]
        {
            td.dtr11
        });

        await DataSource.UpdateAsync(new[]
        {
            td.dt11State, td.dtr11State
        });

        await DataSource.UpdateAsync(new[]
        {
            td.dt11SingleParameter
        });

        await DataSource.UpdateAsync(new[]
        {
            td.dt11Inter
        });


        await Import.FromString(td.amountType).WithType<AmountType>().WithTarget(DataSource).ExecuteAsync();
        await Import.FromString(td.estimateType).WithType<EstimateType>().WithTarget(DataSource).ExecuteAsync();
        await Import.FromString(td.economicBasis).WithType<EconomicBasis>().WithTarget(DataSource).ExecuteAsync();


        await Import.FromString(td.projectionConfiguration).WithType<ProjectionConfiguration>(
        ).WithTarget(DataSource).ExecuteAsync();


        await DataSource.UpdateAsync(new[]
        {
            td.yieldCurve, td.yieldCurvePrevious
        });


        Workspace.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());


        await DataSource.UpdateAsync<PartitionByReportingNode>(td.partitionReportingNode.RepeatOnce());
        await DataSource.UpdateAsync<PartitionByReportingNodeAndPeriod>(td.previousPeriodPartition.RepeatOnce());
        await DataSource.UpdateAsync<PartitionByReportingNodeAndPeriod>(td.partition.RepeatOnce());
    }


    public async Task<List<string>> ErrorLoggerAsync(IEnumerable<IfrsVariable> csm,
        IEnumerable<IfrsVariable> loss,
        Dictionary<AocStep, (double valueCsm, double valueLoss)> csmBenchmark,
        Dictionary<AocStep, (double valueCsm, double valueLoss)> lossBenchmark)
    {
        var errors = new List<string>();
        if (csm.Count() > csmBenchmark.Count())
        {
            var extraVariables = csm.Where(x => !csmBenchmark.Keys.Contains(new AocStep(x.AocType, x.Novelty)))
                .Select(x =>
                    $"AocType:{x.AocType}, Novelty:{x.Novelty}, EstimateType:{x.EstimateType}, Value:{x.Values[0]}.");
            errors.Add(
                $"Computed variable for CSM has more non zero items than benchmark. Extra computed variables : \n {string.Join("\n", extraVariables)}.");
        }

        if (loss.Count() > lossBenchmark.Count())
        {
            var extraVariables = loss.Where(x => !lossBenchmark.Keys.Contains(new AocStep(x.AocType, x.Novelty)))
                .Select(x =>
                    $"AocType:{x.AocType}, Novelty:{x.Novelty}, EstimateType:{x.EstimateType}, Value:{x.Values[0]}, ");
            errors.Add(
                $"Computed variable for LOSS(LC/LoReCo) has more non zero items compared to benchmark: \n {string.Join("\n", extraVariables)}.");
        }

        //Check ValueCsm
        foreach (var kvp in csmBenchmark)
        {
            var variableSwitch = csm.SingleOrDefault(y => y.AocType == kvp.Key.AocType && y.Novelty == kvp.Key.Novelty);

            if (variableSwitch == null)
            {
                errors.Add(
                    $"Missing calculated variable for AocType and Novelty: {kvp.Key.AocType}, {kvp.Key.Novelty}.");
                continue;
            }

            if (Math.Abs(variableSwitch.Values[0] - kvp.Value.valueCsm) > Consts.Precision)
                errors.Add(
                    $"Values not matching for AocType {kvp.Key.AocType} and Novelty {kvp.Key.Novelty}. Variable: Csm {variableSwitch.Values[0]}; Benchmark: Csm {kvp.Value.valueCsm}.");
        }

        //Check ValueLoss
        foreach (var kvp in lossBenchmark)
        {
            var variableSwitch =
                loss.SingleOrDefault(y => y.AocType == kvp.Key.AocType && y.Novelty == kvp.Key.Novelty);

            if (variableSwitch == null)
            {
                errors.Add(
                    $"Missing calculated variable for AocType and Novelty: {kvp.Key.AocType}, {kvp.Key.Novelty}.");
                continue;
            }

            if (Math.Abs(variableSwitch.Values[0] - kvp.Value.valueLoss) > Consts.Precision)
                errors.Add(
                    $"Values not matching for AocType {kvp.Key.AocType} and Novelty {kvp.Key.Novelty}. Variable: Lc/LoReCo {variableSwitch.Values[0]}; Benchmark: Lc/LoReCo {kvp.Value.valueLoss}.");
        }

        return errors;
    }


    public async Task<(IEnumerable<IfrsVariable>, IEnumerable<IfrsVariable>)> ComputeScopesAsync(
        IEnumerable<IfrsVariable> inputDataSet,
        Guid partitionId,
        string primaryDataNode)
    {
        await Workspace.DeleteAsync<IfrsVariable>(await Workspace.Query<IfrsVariable>().ToArrayAsync());
        await Workspace.UpdateAsync<IfrsVariable>(inputDataSet);

        var partition =
            (await Workspace.Query<PartitionByReportingNodeAndPeriod>().ToArrayAsync())
            .Single(x => x.Id == partitionId);
        var args = new ImportArgs(partition.ReportingNode, partition.Year, partition.Month, Periodicity.Quarterly,
            partition.Scenario, ImportFormats.Actual);
        var testStorage = new ImportStorage(args, DataSource, Workspace);
        await testStorage.InitializeAsync();
        var testUniverse = Scopes.ForStorage(testStorage).ToScope<IModel>();
        var identities = testUniverse
            .GetScopes<GetIdentities>(testStorage.DataNodesByImportScope[ImportScope.Primary]
                .Where(dn => dn == primaryDataNode)).SelectMany(s => s.Identities);

        var tmToIfrsVariable = testUniverse.GetScopes<TmToIfrsVariable>(identities);
        var csm = tmToIfrsVariable.SelectMany(x => x.Csms).Where(x => Math.Abs(x.Values[0]) > Consts.Precision);
        var loss = tmToIfrsVariable.SelectMany(x => x.Loss).Where(x => Math.Abs(x.Values[0]) > Consts.Precision);

        await Workspace.DeleteAsync<IfrsVariable>(await Workspace.Query<IfrsVariable>().ToArrayAsync());
        return (csm, loss);
    }


    public async Task<ActivityLog> CheckSwitchLogicAsync(IEnumerable<IfrsVariable> inputDataSet,
        Dictionary<AocStep, (double valueCsm, double valueLoss)> csmLcSwitchBenchmark,
        Dictionary<AocStep, (double valueCsm, double valueLoss)> reinsuranceCsmLcSwitchBenchmark = null)
    {
        Activity.Start();
        //SET UP COMPUTATION
        var gross = (await DataSource.Query<GroupOfInsuranceContract>().ToArrayAsync()).Select(x => x.SystemName);
        var dn = inputDataSet.Select(x => x.DataNode).ToHashSet();
        var dnByType =
            dn.ToDictionary(
                x => gross.Contains(x) ? nameof(GroupOfInsuranceContract) : nameof(GroupOfReinsuranceContract), x => x);
        var partitionId = inputDataSet.First().Partition;

        //Gross
        var errorsGross = new List<string>();
        if (dnByType.TryGetValue(nameof(GroupOfInsuranceContract), out var primaryDataNode))
        {
            //set up bm
            var csmBenchmark = csmLcSwitchBenchmark.Where(x => Math.Abs(x.Value.valueCsm) > Consts.Precision)
                .ToDictionary(x => x.Key, x => x.Value);
            var lossBenchmark = csmLcSwitchBenchmark.Where(x => Math.Abs(x.Value.valueLoss) > Consts.Precision)
                .ToDictionary(x => x.Key, x => x.Value);
            //Set up import storage and test universe
            var (csm, loss) = await ComputeScopesAsync(inputDataSet, partitionId, primaryDataNode);
            errorsGross = await ErrorLoggerAsync(csm, loss, csmBenchmark, lossBenchmark);
        }

        //Reinsurance 
        var errorsReins = new List<string>();
        if (dnByType.TryGetValue(nameof(GroupOfReinsuranceContract), out primaryDataNode))
        {
            //set up bm
            var csmBenchmark = reinsuranceCsmLcSwitchBenchmark.Where(x => Math.Abs(x.Value.valueCsm) > Consts.Precision)
                .ToDictionary(x => x.Key, x => x.Value);
            var lossBenchmark = reinsuranceCsmLcSwitchBenchmark.Where(x => Math.Abs(x.Value.valueLoss) > Consts.Precision)
                .ToDictionary(x => x.Key, x => x.Value);
            //Set up import storage and test universe
            var (csm, loss) = await ComputeScopesAsync(inputDataSet, partitionId, primaryDataNode);
            errorsReins = await ErrorLoggerAsync(csm, loss, csmBenchmark, lossBenchmark);
        }

        //Clean up Workspace
        await Workspace.DeleteAsync<IfrsVariable>(await Workspace.Query<IfrsVariable>().ToArrayAsync());
        //await Workspace.DeleteAsync<IfrsVariable>(inputDataSet);

        if (errorsGross.Any()) ApplicationMessage.Log(Error.Generic, string.Join("Gross Errors: \n", errorsGross));
        if (errorsReins.Any())
            ApplicationMessage.Log(Error.Generic, string.Join("Reinsurance Errors : \n", errorsReins));

        return Activity.Finish();
    }

    public async Task Test1Async()
    {
        var basicIfrsVariable = new IfrsVariable
        {
            Partition = td.previousPeriodPartition.Id, 
            DataNode = td.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = "PR", EstimateType = "BE", EconomicBasis = "L"
        };

        var inputDataSet = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {100.0}},
            basicIfrsVariable with {AocType = "CF", Novelty = "N", Values = new double[] {-10.0}},
            //basicIfrsVariable with {AocType = "IA", Novelty = "N", Values = new double[] {15.0}},
            basicIfrsVariable with {AocType = "EV", Novelty = "N", Values = new double[] {100.0}},
            basicIfrsVariable with {AocType = "CL", Novelty = "C", Values = new double[] {100.0}},
            basicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },
        };

        var csmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "N"), (0d, 100d)},
            {new AocStep("IA", "N"), (0d, 0.2)},
            {new AocStep("EV", "N"), (0d, 100d)},
            {new AocStep("CL", "C"), (0d, 100d)},
            {new AocStep("EA", "C"), (0d, -8.0)},
            {new AocStep("AM", "C"), (0d, -146.1)},
            {new AocStep("EOP", "C"), (0d, 146.1)},
        };


        var activity = await CheckSwitchLogicAsync(inputDataSet, csmLcSwitch_benchmark);



        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test2Async()
    {
        var basicIfrsVariable = new IfrsVariable
        {
            Partition = td.previousPeriodPartition.Id, DataNode = td.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = "PR", EstimateType = "BE", EconomicBasis = "L"
        };

        var inputDataSet = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {100.0}},
            basicIfrsVariable with {AocType = "CF", Novelty = "N", Values = new double[] {-10.0}},
            //basicIfrsVariable with {AocType = "IA", Novelty = "N", Values = new double[] {15.0}},
            basicIfrsVariable with {AocType = "EV", Novelty = "N", Values = new double[] {100.0}},
            basicIfrsVariable with {AocType = "CL", Novelty = "C", Values = new double[] {-500.0}},
            basicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },
        };

        var csmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "N"), (0d, 100d)},
            {new AocStep("IA", "N"), (0d, 0.2)},
            {new AocStep("EV", "N"), (0d, 100d)},
            {new AocStep("CL", "C"), (299.8, -200.2)},
            {new AocStep("EA", "C"), (8d, 0d)},
            {new AocStep("AM", "C"), (-153.9d, 0d)},
            {new AocStep("EOP", "C"), (153.9, 0d)},
        };


        var activity = await CheckSwitchLogicAsync(inputDataSet, csmLcSwitch_benchmark);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test3Async()
    {
        var basicIfrsVariable = new IfrsVariable
        {
            Partition = td.previousPeriodPartition.Id, 
            DataNode = td.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = "PR", EstimateType = "BE", EconomicBasis = "L"
        };

        var inputDataSet = new IfrsVariable[]
        {
            basicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {100.0}},
            basicIfrsVariable with {AocType = "CF", Novelty = "N", Values = new double[] {-10.0}},
            basicIfrsVariable with {AocType = "MC", Novelty = "N", Values = new double[] {-150.0}},
            basicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },
        };

        var csmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "N"), (0d, 100d)},
            {new AocStep("MC", "N"), (50d, -100d)},
            {new AocStep("IA", "N"), (0.1, 0)},
            {new AocStep("EA", "C"), (8d, 0d)},
            {new AocStep("AM", "C"), (-29.05, 0d)},
            {new AocStep("EOP", "C"), (29.05, 0d)},
        };


        var activity = await CheckSwitchLogicAsync(inputDataSet, csmLcSwitch_benchmark);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test4Async()
    {
        var basicIfrsVariable = new IfrsVariable
        {
            Partition = td.previousPeriodPartition.Id, 
            DataNode = td.groupOfInsuranceContracts, AccidentYear = null,
            AmountType = "PR", EstimateType = "BE", EconomicBasis = "L"
        };

        var inputDataSet = new IfrsVariable[]
        {
            basicIfrsVariable with
            {
                AocType = "BOP", Novelty = "I", Values = new double[] {5010.0}, EstimateType = "L", AmountType = null
            },
            basicIfrsVariable with {AocType = "MC", Novelty = "I", Values = new double[] {-10.0}},
            basicIfrsVariable with {AocType = "EV", Novelty = "I", Values = new double[] {-5015.0}},
            basicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {-100.0}},
            basicIfrsVariable with {AocType = "CF", Novelty = "N", Values = new double[] {10.0}},
            basicIfrsVariable with {AocType = "MC", Novelty = "N", Values = new double[] {150.0}},
            basicIfrsVariable with {AocType = "EV", Novelty = "N", Values = new double[] {-45.0}},
            basicIfrsVariable with {AocType = "CL", Novelty = "C", Values = new double[] {-30.0}},
            basicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },
        };

        var csmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "I"), (0d, 5010.0)},
            {new AocStep("MC", "I"), (0d, -10d)},
            {new AocStep("IA", "I"), (0d, 10d)},
            {new AocStep("EV", "I"), (5d, -5010d)},

            {new AocStep("BOP", "N"), (100d, 0d)},
            {new AocStep("MC", "N"), (-100d, 50d)},
            {new AocStep("IA", "N"), (0d, 0.1)},
            {new AocStep("EV", "N"), (0d, -45d)},

            {new AocStep("CL", "C"), (24.9d, -5.1)},
            {new AocStep("EA", "C"), (-8d, 0d)},
            {new AocStep("AM", "C"), (-10.95, 0d)},
            {new AocStep("EOP", "C"), (10.95, 0d)},
        };


        var activity = await CheckSwitchLogicAsync(inputDataSet, csmLcSwitch_benchmark);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test5Async()
    {
        var reinsBasicIfrsVariable = new IfrsVariable
        {
            Partition = td.partition.Id, 
            DataNode = td.groupOfReinsuranceContracts, AccidentYear = null, AmountType = "PR",
            EstimateType = "BE", EconomicBasis = "L"
        };

        var grossBasicIfrsVariable = new IfrsVariable
        {
            Partition = td.partition.Id, 
            DataNode = td.groupOfInsuranceContracts, AccidentYear = null, AmountType = "Cl",
            EstimateType = "BE", EconomicBasis = "L"
        };

        var inputDataSet = new IfrsVariable[]
        {
            grossBasicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {-100.0}},
            grossBasicIfrsVariable with {AocType = "CF", Novelty = "N", Values = new double[] {10.0}},
            //grossBasicIfrsVariable with {AocType = "IA", Novelty = "N", Values = new double[] {-15.0}},
            grossBasicIfrsVariable with {AocType = "EV", Novelty = "N", Values = new double[] {-100.0}},
            grossBasicIfrsVariable with {AocType = "CL", Novelty = "C", Values = new double[] {+100.0}},
            grossBasicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },

            reinsBasicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {100.0}},
            //reinsBasicIfrsVariable with {AocType = "IA", Novelty = "N", Values = new double[] {-10.0}},
            reinsBasicIfrsVariable with {AocType = "CL", Novelty = "C", Values = new double[] {-30.0}},
            reinsBasicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },
        };

        var csmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "N"), (100d, 0d)},
            {new AocStep("IA", "N"), (0.04996, 0d)},
            {new AocStep("EV", "N"), (100d, 0d)},
            {new AocStep("CL", "C"), (-100d, 0d)},
            {new AocStep("AM", "C"), (-50.02498127, 0d)},
            {new AocStep("EOP", "C"), (50.02498127, 0d)},
        };

        var reinsCsmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "N"), (-100d, 0d)},
            {new AocStep("IA", "N"), (-0.04996, 0d)},
            {new AocStep("CL", "C"), (+30d, 0d)},
            {new AocStep("AM", "C"), (35.02498, 0d)},
            {new AocStep("EOP", "C"), (-35.02498, 0d)},
        };


        var activity = await CheckSwitchLogicAsync(inputDataSet, csmLcSwitch_benchmark, reinsCsmLcSwitch_benchmark);



        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test6Async()
    {
        var reinsBasicIfrsVariable = new IfrsVariable
        {
            Partition = td.partition.Id, 
            DataNode = td.groupOfReinsuranceContracts, AccidentYear = null, AmountType = "PR",
            EstimateType = "BE", EconomicBasis = "L"
        };

        var grossBasicIfrsVariable = new IfrsVariable
        {
            Partition = td.partition.Id, DataNode = td.groupOfInsuranceContracts, AccidentYear = null, AmountType = "PR",
            EstimateType = "BE", EconomicBasis = "L"
        };

        var inputDataSet = new IfrsVariable[]
        {
            grossBasicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {100.0}},
            grossBasicIfrsVariable with {AocType = "CF", Novelty = "N", Values = new double[] {-10.0}},
            grossBasicIfrsVariable with {AocType = "MC", Novelty = "N", Values = new double[] {-150.0}},
            grossBasicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },

            reinsBasicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {-100.0}},
            //reinsBasicIfrsVariable with {AocType = "IA", Novelty = "N", Values = new double[] {-10.0}},
            reinsBasicIfrsVariable with {AocType = "CL", Novelty = "C", Values = new double[] {-30.0}},
            reinsBasicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },
        };

//Gross CSM-LC
        var csmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "N"), (0d, 100d)},
            {new AocStep("MC", "N"), (50d, -100d)},
            {new AocStep("IA", "N"), (0.0249812, 0)},
            {new AocStep("EA", "C"), (8d, 0d)},
            {new AocStep("AM", "C"), (-29.0124906, 0d)},
            {new AocStep("EOP", "C"), (29.0124906, 0d)},
        };

        var reinsCsmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "N"), (+100d, -100d)},
            {new AocStep("MC", "N"), (+0d, 100d)},
            {new AocStep("IA", "N"), (0.04996254, 0)},
            {new AocStep("CL", "C"), (+30d, 0d)},
            {new AocStep("AM", "C"), (-65.0249812, 0d)},
            {new AocStep("EOP", "C"), (65.0249812, 0d)},
        };


        var activity = await CheckSwitchLogicAsync(inputDataSet, csmLcSwitch_benchmark, reinsCsmLcSwitch_benchmark);



        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test7Async()
    {
        var reinsBasicIfrsVariable = new IfrsVariable
        {
            Partition = td.partition.Id, DataNode = td.groupOfReinsuranceContracts, AccidentYear = null, AmountType = "PR",
            EstimateType = "BE", EconomicBasis = "L"
        };

        var grossBasicIfrsVariable = new IfrsVariable
        {
            Partition = td.partition.Id, DataNode = td.groupOfInsuranceContracts, AccidentYear = null, AmountType = "Cl",
            EstimateType = "BE", EconomicBasis = "L"
        };

        var inputDataSet = new IfrsVariable[]
        {
            grossBasicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {100.0}},
            grossBasicIfrsVariable with {AocType = "MC", Novelty = "N", Values = new double[] {-50.0}},
            grossBasicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },

            reinsBasicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {-100.0}},
            reinsBasicIfrsVariable with {AocType = "MC", Novelty = "N", Values = new double[] {-10.0}},
            reinsBasicIfrsVariable with {AocType = "CL", Novelty = "C", Values = new double[] {-30.0}},
            reinsBasicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },
        };

//Gross CSM-LC
        var csmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "N"), (0d, 100d)},
            {
                new AocStep("MC", "N"), (0d, -50d)
            }, //MC of the gross has opposite sign wrt the others (triggers switching)
            {new AocStep("IA", "N"), (0d, 0.024981)},
            {new AocStep("AM", "C"), (0d, -25.01249)},
            {new AocStep("EOP", "C"), (0d, 25.01249)},
        };

        var reinsCsmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "N"), (+100d, -100d)},
            {new AocStep("MC", "N"), (+10d, 50d)},
            {new AocStep("IA", "N"), (0.054958, -0.024981)},
            {new AocStep("CL", "C"), (+30d, 0d)},
            {new AocStep("AM", "C"), (-70.02747, 25.01249)},
            {new AocStep("EOP", "C"), (70.02747, -25.01249)},
        };


        var activity = await CheckSwitchLogicAsync(inputDataSet, csmLcSwitch_benchmark, reinsCsmLcSwitch_benchmark);



        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test8Async()
    {
        var reinsBasicIfrsVariable = new IfrsVariable
        {
            Partition = td.partition.Id, DataNode = td.groupOfReinsuranceContracts, AccidentYear = null, AmountType = "PR",
            EstimateType = "BE", EconomicBasis = "L"
        };

        var grossBasicIfrsVariable = new IfrsVariable
        {
            Partition = td.partition.Id, DataNode = td.groupOfInsuranceContracts, AccidentYear = null, AmountType = "Cl",
            EstimateType = "BE", EconomicBasis = "L"
        };

        var inputDataSet = new IfrsVariable[]
        {
            grossBasicIfrsVariable with
            {
                AocType = "BOP", Novelty = "I", Values = new double[] {100.0}, EstimateType = "C", AmountType = null
            },
            grossBasicIfrsVariable with {AocType = "IA", Novelty = "I", Values = new double[] {10.0}},
            grossBasicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {50.0}},
            //grossBasicIfrsVariable with {AocType = "IA", Novelty = "N", Values = new double[] {-10.0}}, 
            grossBasicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },

            reinsBasicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {-100.0}},
            //reinsBasicIfrsVariable with {AocType = "IA", Novelty = "N", Values = new double[] {-10.0}},
            reinsBasicIfrsVariable with {AocType = "CL", Novelty = "C", Values = new double[] {-30.0}},
            reinsBasicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },
        };

//Gross CSM-LC
        var csmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "I"), (100d, 0d)},
            {new AocStep("IA", "I"), (0.049962543d, 0d)},
            {new AocStep("BOP", "N"), (0d, 50d)},
            {new AocStep("IA", "N"), (0d, 0.0249812718)},
            {new AocStep("CL", "C"), (-50.024981, -50.024981)},
            {new AocStep("AM", "C"), (-25.012490, 0d)},
            {new AocStep("EOP", "C"), (25.012490, 0d)},
        };

        var reinsCsmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "N"), (100d, -50d)},
            {new AocStep("IA", "N"), (0.04996254, -0.02498127)},
            {new AocStep("CL", "C"), (30d, 50.0249812)},
            {new AocStep("AM", "C"), (-65.0249812, 0d)},
            {new AocStep("EOP", "C"), (65.0249812, 0d)},
        };


        var activity = await CheckSwitchLogicAsync(inputDataSet, csmLcSwitch_benchmark, reinsCsmLcSwitch_benchmark);



        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test9Async()
    {
        var reinsBasicIfrsVariable = new IfrsVariable
        {
            Partition = td.partition.Id, DataNode = td.groupOfReinsuranceContracts, AccidentYear = null, AmountType = "PR",
            EstimateType = "BE", EconomicBasis = "L"
        };

        var grossBasicIfrsVariable = new IfrsVariable
        {
            Partition = td.partition.Id, DataNode = td.groupOfInsuranceContracts, AccidentYear = null, AmountType = "Cl",
            EstimateType = "BE", EconomicBasis = "L"
        };

        var inputDataSet = new IfrsVariable[]
        {
            grossBasicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {20.0}},
            grossBasicIfrsVariable with {AocType = "MC", Novelty = "N", Values = new double[] {10.0}},
            //grossBasicIfrsVariable with {AocType = "IA", Novelty = "N", Values = new double[] {-5.0}}, 
            grossBasicIfrsVariable with {AocType = "AU", Novelty = "N", Values = new double[] {5.0}},
            grossBasicIfrsVariable with {AocType = "EV", Novelty = "N", Values = new double[] {5.0}},
            grossBasicIfrsVariable with {AocType = "CL", Novelty = "C", Values = new double[] {-5.0}},
            grossBasicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {1}, EstimateType = "F", AmountType = "CU"
            },

            reinsBasicIfrsVariable with {AocType = "BOP", Novelty = "N", Values = new double[] {-10.0}},
            reinsBasicIfrsVariable with {AocType = "MC", Novelty = "N", Values = new double[] {19.0}},
            //reinsBasicIfrsVariable with {AocType = "IA", Novelty = "N", Values = new double[] {-44.0}},
            reinsBasicIfrsVariable with {AocType = "AU", Novelty = "N", Values = new double[] {15.0}},
            reinsBasicIfrsVariable with {AocType = "EV", Novelty = "N", Values = new double[] {55.0}},
            reinsBasicIfrsVariable with {AocType = "CL", Novelty = "C", Values = new double[] {0.0}},
            reinsBasicIfrsVariable with
            {
                AocType = "AM", Novelty = "C", Values = new double[] {0.5}, EstimateType = "F", AmountType = "CU"
            },
        };

//Gross CSM-LC
        var csmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "N"), (0d, 20d)},
            {new AocStep("MC", "N"), (0d, 10d)},
            {new AocStep("IA", "N"), (0d, 0.014988763)},
            {new AocStep("AU", "N"), (0d, 5d)},
            {new AocStep("EV", "N"), (0d, 5d)},
            {new AocStep("CL", "C"), (0d, -5d)},
            {new AocStep("AM", "C"), (0d, -35.0149887)},
            {new AocStep("EOP", "C"), (0d, 0d)},
        };

        var reinsCsmLcSwitch_benchmark = new Dictionary<AocStep, (double valueCsm, double valueLc)>()
        {
            {new AocStep("BOP", "N"), (10d, -20d)},
            {new AocStep("MC", "N"), (-19d, 19d)},
            {new AocStep("IA", "N"), (-0.00449662, 0.0044966)},
            {new AocStep("AU", "N"), (-15d, 0.9955033)},
            {new AocStep("EV", "N"), (-55d, 0d)},
            {new AocStep("CL", "C"), (0d, 0d)},
            {new AocStep("AM", "C"), (39.502248, 0d)},
            {new AocStep("EOP", "C"), (-39.502248, 0d)},
        };


        var activity = await CheckSwitchLogicAsync(inputDataSet, csmLcSwitch_benchmark, reinsCsmLcSwitch_benchmark);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }
}



