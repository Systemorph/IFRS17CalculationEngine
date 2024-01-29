using FluentAssertions;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Import;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Import;
using Systemorph.Vertex.Scopes.Proxy;
using Systemorph.Vertex.Workspace;
using Error = OpenSmc.Ifrs17.Domain.Constants.Error;

namespace OpenSmc.Ifrs17.Domain.Test;

public class AocStructureTest : TestBase
{
    private RawVariable[]? inputRawVariables;
    private Workspace? workspace;
    private Dictionary<AocStep, IEnumerable<AocStep>>? ParentBm { get; set; }
    private Dictionary<AocStep, IEnumerable<AocStep>>? FullAocBm { get; set; }
    private Dictionary<AocStep, IEnumerable<AocStep>>? ReferenceBm { get; set; }
    private Dictionary<AocStep, IEnumerable<AocStep>>? ParentBmCDr { get; set; }

    public AocStructureTest(IImportVariable import, IDataSource dataSource,
        IWorkspaceVariable work, IActivityVariable activity, IScopeFactory scopes) :
        base(import, work, scopes)
    {
        workspace = Work.CreateNew() as Workspace;
    }

    private async Task InitializeDataSourceAsync()
    {
        await TestData.InitializeAsync();
        await DataSource.DeleteAsync(DataSource.Query<AocType>());
        await DataSource.DeleteAsync(DataSource.Query<AocConfiguration>());
        await Import.FromString(TestData.ProjectionConfiguration).WithType<ProjectionConfiguration>()
            .WithTarget(DataSource)
            .ExecuteAsync();
        await DataSource.UpdateAsync<Portfolio>(TestData.Dt1.RepeatOnce());
        await DataSource.UpdateAsync<Portfolio>(TestData.Dtr1.RepeatOnce());
        await DataSource.UpdateAsync<GroupOfInsuranceContract>(TestData.Dt11.RepeatOnce());
        await DataSource.UpdateAsync<GroupOfReinsuranceContract>(TestData.Dtr11.RepeatOnce());

        await DataSource.UpdateAsync(new[] {TestData.Dt11State, TestData.Dtr11State});
        await DataSource.UpdateAsync(TestData.Dt11Inter.RepeatOnce());


        await Import.FromString(TestData.EstimateType)
            .WithType<EstimateType>()
            .WithTarget(DataSource)
            .ExecuteAsync();

        await DataSource.UpdateAsync(TestData.YieldCurvePrevious.RepeatOnce());
        workspace.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>()
            .DisableInitialization<IfrsVariable>());


        await DataSource.UpdateAsync(new[]
        {
            TestData.Partition, TestData.PreviousPeriodPartition
        });

        await DataSource.UpdateAsync(TestData.PartitionReportingNode.RepeatOnce());

        inputRawVariables = new RawVariable[]
        {
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "BOP",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "MC",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "BOP",
                Novelty = "N", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "EV",
                Novelty = "N", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "CL",
                Novelty = "C", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
        };
    }


    private async Task<ActivityLog> CheckAocStepStructureAsync(IEnumerable<BaseDataRecord> inputVariables,
        IActivityVariable activity,
        Dictionary<AocStep, IEnumerable<AocStep>> parentBmInner,
        Dictionary<AocStep, IEnumerable<AocStep>> referenceBm,
        Dictionary<AocStep, IEnumerable<AocStep>> fullAocBmInner,
        StructureType structureType = StructureType.AocPresentValue,
        Dictionary<AocStep, IEnumerable<AocStep>> parentBmCdr = null)
    {
        activity.Start();
        //Save test input data
        var importFormat = ImportFormats.Cashflow;
        var inputSource = InputSource.Cashflow;
        if (inputVariables.First() is RawVariable)
        {
            await workspace.UpdateAsync<RawVariable>(inputVariables.Cast<RawVariable>());
        }

        if (inputVariables.First() is IfrsVariable)
        {
            await workspace.UpdateAsync<IfrsVariable>(inputVariables.Cast<IfrsVariable>());
            importFormat = ImportFormats.Actual;
            inputSource = InputSource.Actual;
        }

        var newArgs = TestData.Args with {ImportFormat = importFormat};
        var goc = inputVariables.First().DataNode;

        //Set up import storage and test universe
        var testStorage = new ImportStorage(newArgs, DataSource, workspace);
        await testStorage.InitializeAsync();
        var isReinsurance = testStorage.DataNodeDataBySystemName[goc].IsReinsurance;
        var testUniverse = Scopes.ForStorage(testStorage).ToScope<IModel>();
        var identities = testUniverse
            .GetScopes<GetIdentities>(testStorage.DataNodesByImportScope[ImportScope.Primary].Where(dn => dn == goc))
            .SelectMany(s => s.Identities);

        //Clean up Workspace
        await workspace.DeleteAsync<RawVariable>(await workspace.Query<RawVariable>().ToArrayAsync());
        await workspace.DeleteAsync<IfrsVariable>(await workspace.Query<IfrsVariable>().ToArrayAsync());

        var errors = new List<string>();

        //Assert Parents
        if (importFormat != ImportFormats.Actual)
        {
            var parents = testUniverse
                .GetScopes<ParentAocStep>(identities.Select(id => (object) (id, "PR", structureType)),
                    o => o.WithStorage(testStorage)).Where(x => x.Values.Any()).ToArray();
            if (parentBmInner.Count() != parents.Count())
            {
                var computedIds =
                    parents.Select(s => $"AocType:{s.Identity.Id.AocType}, Novelty:{s.Identity.Id.Novelty}");
                var expectedIds = parentBmInner.Keys.Select(aoc => $"AocType:{aoc.AocType}, Novelty:{aoc.Novelty}");
                errors.Add(
                    $"Parent count does not match expected: \n Computed {parents.Count()} \n {string.Join("\n", computedIds)} \n Expected {parentBmInner.Count()} \n {string.Join("\n", expectedIds)}.");
            }

            foreach (var kvp in parentBmInner)
            {
                var scopeParents = parents.Where(y =>
                    y.Identity.Id.AocType == kvp.Key.AocType && y.Identity.Id.Novelty == kvp.Key.Novelty);
                if (scopeParents.Count() != 1)
                    errors.Add($"Parent not found for AocStep: {kvp.Key.AocType}, {kvp.Key.Novelty}.");
                else
                {
                    var scopeParent = scopeParents.First();
                    if (kvp.Value.Intersect(scopeParent.Values).Count() != kvp.Value.Count() ||
                        kvp.Value.Intersect(scopeParent.Values).Count() != scopeParent.Values.Count())
                    {
                        var computedAocSteps =
                            scopeParent.Values.Select(aoc => $"AocType:{aoc.AocType}, Novelty:{aoc.Novelty}");
                        var expectedAocSteps = kvp.Value.Select(aoc => $"AocType:{aoc.AocType}, Novelty:{aoc.Novelty}");
                        errors.Add(
                            $"Parents of {kvp.Key.AocType}, {kvp.Key.Novelty} do not match expected value: \n Computed: \n {string.Join("\n", computedAocSteps)} \n Expected: \n {string.Join("\n", expectedAocSteps)}.");
                    }
                }
            }

            //Parents for CDR
            if (isReinsurance)
            {
                var parentsCdr = testUniverse
                    .GetScopes<ParentAocStep>(identities.Select(id => (object) (id, AmountTypes.CDR, structureType)),
                        o => o.WithStorage(testStorage)).ToArray();

                var countP = parentsCdr.Where(x => x.Values.Any()).Count();
                if (parentBmCdr.Count() != countP)
                    errors.Add(
                        $"Parent count for AmountType CDR does not match expected: \n Computed {countP} \n Expected {parentBmInner.Count()}.");

                foreach (var kvp in parentBmCdr)
                {
                    var scopeParents = parentsCdr.Where(y =>
                        y.Identity.Id.AocType == kvp.Key.AocType && y.Identity.Id.Novelty == kvp.Key.Novelty);
                    if (scopeParents.Count() != 1)
                        errors.Add($"Parent for CDR not found for AocStep: {kvp.Key.AocType}, {kvp.Key.Novelty}.");
                    else
                    {
                        var scopeParent = scopeParents.First();
                        if (kvp.Value.Intersect(scopeParent.Values).Count() != kvp.Value.Count() ||
                            kvp.Value.Intersect(scopeParent.Values).Count() != scopeParent.Values.Count())
                        {
                            var computedAocSteps =
                                scopeParent.Values.Select(aoc => $"AocType:{aoc.AocType}, Novelty:{aoc.Novelty}");
                            var expectedAocSteps =
                                kvp.Value.Select(aoc => $"AocType:{aoc.AocType}, Novelty:{aoc.Novelty}");
                            errors.Add(
                                $"Parents of {kvp.Key.AocType}, {kvp.Key.Novelty} for AmountType CDR do not match expected value: \n Computed: \n {string.Join("\n", computedAocSteps)} \n Expected: \n {string.Join("\n", expectedAocSteps)}.");
                        }
                    }
                }
            }
        }

        //Assert Reference
        if (importFormat != ImportFormats.Actual)
        {
            var reference = testUniverse.GetScopes<ReferenceAocStep>(identities, o => o.WithStorage(testStorage))
                .ToArray();
            var countR = reference.Select(x => x.Values).Count();
            if (referenceBm.Count() != countR)
                errors.Add(
                    $"Reference count does not match expected: \n Computed {countR} \n Expected {referenceBm.Count()}.");

            foreach (var kvp in referenceBm)
            {
                var scopeReferences = reference.Where(y =>
                    y.Identity.AocType == kvp.Key.AocType && y.Identity.Novelty == kvp.Key.Novelty);
                if (!scopeReferences.Any())
                    errors.Add($"Reference not found for AocStep: {kvp.Key.AocType}, {kvp.Key.Novelty}.");
                else
                {
                    var scopeReference = scopeReferences.First();
                    if (kvp.Value.Intersect(scopeReference.Values).Count() != kvp.Value.Count() ||
                        kvp.Value.Intersect(scopeReference.Values).Count() != scopeReference.Values.Count())
                    {
                        var computedAocSteps =
                            scopeReference.Values.Select(aoc => $"AocType:{aoc.AocType}, Novelty:{aoc.Novelty}");
                        var expectedAocSteps = kvp.Value.Select(aoc => $"AocType:{aoc.AocType}, Novelty:{aoc.Novelty}");
                        errors.Add(
                            $"Reference of {kvp.Key.AocType}, {kvp.Key.Novelty} do not match expected value: \n Computed: \n {string.Join("\n", computedAocSteps)} \n Expected: \n {string.Join("\n", expectedAocSteps)}.");
                    }
                }
            }
        }

        //Assert FullAoc
        var fullAoc = testUniverse
            .GetScopes<PreviousAocSteps>(identities.Select(id => (object) (id, structureType)),
                o => o.WithStorage(testStorage)).Where(s => s.Values.Any());
        var count = fullAoc.Count();
        if (fullAocBmInner.Count() != count)
        {
            var computedAocSteps =
                fullAoc.Select(x => $"AocType:{x.Identity.Id.AocType}, Novelty:{x.Identity.Id.Novelty}");
            var benchmarkKeys = fullAocBmInner.Keys.Select(aoc => $"AocType:{aoc.AocType}, Novelty:{aoc.Novelty}");
            errors.Add(
                $"Full AoC count does not match expected: \n Computed {count} \n Expected {fullAocBmInner.Count()}.");
            errors.Add(
                $"In particular, \n Computed Identities \n {string.Join("\n", computedAocSteps)} \n Expected \n {string.Join("\n", benchmarkKeys)}.");
        }
        else
            foreach (var kvp in fullAocBmInner)
            {
                var scopeAocFulls = fullAoc.Where(y =>
                    y.Identity.Id.AocType == kvp.Key.AocType && y.Identity.Id.Novelty == kvp.Key.Novelty);
                if (scopeAocFulls.Count() != 1)
                {
                    errors.Add($"Full AocStep not found for AocStep: {kvp.Key.AocType}, {kvp.Key.Novelty}.");
                }
                else
                {
                    var scopeAocFull = scopeAocFulls.First();
                    if (kvp.Value.Intersect(scopeAocFull.Values).Count() != kvp.Value.Count() ||
                        kvp.Value.Intersect(scopeAocFull.Values).Count() != scopeAocFull.Values.Count())
                    {
                        var computedAocSteps =
                            scopeAocFull.Values.Select(aoc => $"AocType:{aoc.AocType}, Novelty:{aoc.Novelty}");
                        var expectedAocSteps = kvp.Value.Select(aoc => $"AocType:{aoc.AocType}, Novelty:{aoc.Novelty}");
                        errors.Add(
                            $"AocFull for {kvp.Key.AocType}, {kvp.Key.Novelty} do not match expected value: \n Computed \n {string.Join("\n", computedAocSteps)} \n Expected: \n {string.Join("\n", expectedAocSteps)}.");
                    }
                }
            }

        if (errors.Any()) ApplicationMessage.Log(Error.Generic, string.Join("\n", errors));
        return activity.Finish();
    }


    [Fact]
    public async Task FirstCheckAsync()
    {
        var fullAocBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}},
            {new AocStep("CF", "I"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("MC", "I")}},
            {
                new AocStep("IA", "I"),
                new AocStep[] {new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I")}
            },
            {
                new AocStep("YCU", "I"),
                new AocStep[]
                    {new AocStep("IA", "I"), new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I")}
            },
            {new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N"), new AocStep("CF", "N")}},
            {
                new AocStep("EV", "N"),
                new AocStep[] {new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N")}
            },

            {
                new AocStep("CL", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"), new AocStep("EV", "N"),
                }
            },

            {
                new AocStep("EOP", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"), new AocStep("EV", "N"),
                    new AocStep("CL", "C"),
                }
            },
        };

        ParentBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}},
            {new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}},
            {new AocStep("EV", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("CL", "C"), new AocStep[] {new AocStep("YCU", "I"), new AocStep("EV", "N"),}},
        };


        var referenceBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("BOP", "I"), new AocStep[] {new AocStep("BOP", "I")}},
            {new AocStep("MC", "I"), new AocStep[] {new AocStep("MC", "I")}},
            {new AocStep("CF", "I"), new AocStep[] {new AocStep("MC", "I")}},
            {new AocStep("IA", "I"), new AocStep[] {new AocStep("MC", "I")}},
            {new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}},
            {new AocStep("BOP", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("EV", "N"), new AocStep[] {new AocStep("EV", "N")}},
            {new AocStep("CL", "C"), new AocStep[] {new AocStep("CL", "C")}},
            {new AocStep("WO", "C"), new AocStep[] {new AocStep("WO", "C")}},
            {new AocStep("EA", "C"), new AocStep[] {new AocStep("CF", "C")}},
            {new AocStep("AM", "C"), new AocStep[] {new AocStep("CL", "C")}},
            {new AocStep("EOP", "C"), new AocStep[] {new AocStep("CL", "C")}},
            {new AocStep("CF", "C"), new AocStep[] {new AocStep("CF", "C")}},
        };
        var activity = await CheckAocStepStructureAsync(inputRawVariables, Activity, ParentBm, referenceBm,
            fullAocBm,
            StructureType.AocPresentValue);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task SecondCheckAsync()
    {
        inputRawVariables = new RawVariable[]
        {
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfReinsuranceContracts, AocType = "BOP",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfReinsuranceContracts, AocType = "MC",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfReinsuranceContracts, AocType = "BOP",
                Novelty = "N", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfReinsuranceContracts, AocType = "EV",
                Novelty = "N", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfReinsuranceContracts, AocType = "CL",
                Novelty = "C", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
        };


        ParentBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("CRU", "I"), new AocStep[] {new AocStep("YCU", "I")}
            },
            {
                new AocStep("EV", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("CL", "C"), new AocStep[] {new AocStep("YCU", "I"), new AocStep("EV", "N"),}
            },
        };


        var parentBmCdr = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}},
            {new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}},
            {new AocStep("CRU", "I"), new AocStep[] {new AocStep("YCU", "I")}},
            {new AocStep("EV", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("CL", "C"), new AocStep[] {new AocStep("CRU", "I"), new AocStep("EV", "N"),}},
        };


        var referenceBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("BOP", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("RCU", "I"), new AocStep[] { }
            },
            {
                new AocStep("CF", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("IA", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("CRU", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("BOP", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("EV", "N"), new AocStep[] {new AocStep("EV", "N")}
            },
            {
                new AocStep("CL", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("EA", "C"), new AocStep[] {new AocStep("CF", "C")}
            },
            {
                new AocStep("AM", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("EOP", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("CF", "C"), new AocStep[] {new AocStep("CF", "C")}
            },
            {
                new AocStep("WO", "C"), new AocStep[] {new AocStep("WO", "C")}
            },
        };

        FullAocBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("CF", "I"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("MC", "I")}
            },
            {
                new AocStep("IA", "I"), new AocStep[]
                    {new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[]
                    {new AocStep("IA", "I"), new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I")}
            },
            {
                new AocStep("CRU", "I"), new AocStep[]
                {
                    new AocStep("YCU", "I"), new AocStep("IA", "I"), new AocStep("BOP", "I"), new AocStep("MC", "I"),
                    new AocStep("CF", "I")
                }
            },
            {
                new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N"), new AocStep("CF", "N")}
            },
            {
                new AocStep("EV", "N"), new AocStep[]
                    {new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N")}
            },
            {
                new AocStep("CL", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"), new AocStep("CRU", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"), new AocStep("EV", "N"),
                }
            },
            {
                new AocStep("EOP", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"), new AocStep("CRU", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"), new AocStep("EV", "N"),
                    new AocStep("CL", "C"),
                }
            },
        };
        var activity = await CheckAocStepStructureAsync(inputRawVariables, Activity,
            ParentBm, referenceBm, FullAocBm,
            StructureType.AocPresentValue, parentBmCdr);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task ThirdCheckAsync()
    {
        FullAocBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("RCU", "I"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("MC", "I"),}
            },
            {
                new AocStep("IA", "I"), new AocStep[]
                    {new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("RCU", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[]
                    {new AocStep("IA", "I"), new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("RCU", "I")}
            },
            {
                new AocStep("CRU", "I"), new AocStep[]
                {
                    new AocStep("YCU", "I"), new AocStep("IA", "I"), new AocStep("BOP", "I"), new AocStep("MC", "I"),
                    new AocStep("RCU", "I")
                }
            },

            {
                new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("EV", "N"), new AocStep[] {new AocStep("BOP", "N"), new AocStep("IA", "N")}
            },
            {
                new AocStep("CL", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("RCU", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"), new AocStep("CRU", "I"),
                    new AocStep("BOP", "N"), new AocStep("IA", "N"), new AocStep("EV", "N"),
                }
            },
            {
                new AocStep("EA", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("RCU", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"), new AocStep("CRU", "I"),
                    new AocStep("BOP", "N"), new AocStep("IA", "N"), new AocStep("EV", "N"),
                    new AocStep("CL", "C"),
                }
            },

            {
                new AocStep("AM", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("RCU", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"), new AocStep("CRU", "I"),
                    new AocStep("BOP", "N"), new AocStep("IA", "N"), new AocStep("EV", "N"),
                    new AocStep("EA", "C"), new AocStep("CL", "C"),
                }
            },
            {
                new AocStep("EOP", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("RCU", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"), new AocStep("CRU", "I"),
                    new AocStep("BOP", "N"), new AocStep("IA", "N"), new AocStep("EV", "N"),
                    new AocStep("EA", "C"), new AocStep("AM", "C"), new AocStep("CL", "C"),
                }
            },
        };


        var activity = await CheckAocStepStructureAsync(inputRawVariables, Activity, ParentBm,
            ReferenceBm, FullAocBm,
            StructureType.AocTechnicalMargin, ParentBmCDr);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task FourthCheckAsync()
    {
        inputRawVariables = new RawVariable[]
        {
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "BOP",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "MC",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "EV",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "CL",
                Novelty = "C", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
        };


        ParentBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("EV", "I"), new AocStep[] {new AocStep("YCU", "I")}
            },
            {
                new AocStep("CL", "C"), new AocStep[] {new AocStep("EV", "I")}
            },
        };


        var referenceBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("BOP", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("CF", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("IA", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("EV", "I"), new AocStep[] {new AocStep("EV", "I")}
            },
            {
                new AocStep("CL", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("EA", "C"), new AocStep[] {new AocStep("CF", "C")}
            },
            {
                new AocStep("AM", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("EOP", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("CF", "C"), new AocStep[] {new AocStep("CF", "C")}
            },
            {
                new AocStep("WO", "C"), new AocStep[] {new AocStep("WO", "C")}
            },
        };


        var fullAocBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("CF", "I"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("MC", "I")}
            },
            {
                new AocStep("IA", "I"), new AocStep[]
                    {new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[]
                    {new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I")}
            },
            {
                new AocStep("EV", "I"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I")
                }
            },

            {
                new AocStep("CL", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"), new AocStep("EV", "I"),
                }
            },
            {
                new AocStep("EOP", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"), new AocStep("EV", "I"),
                    new AocStep("CL", "C")
                }
            },
        };


        var activity = await CheckAocStepStructureAsync(inputRawVariables, Activity,
            ParentBm, referenceBm, fullAocBm);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task FifthCheckAsync()
    {
        string newAocConfig =
            @"@@AocConfiguration,,,,,,,,,,,
AocType,Novelty,DataType,InputSource,StructureType,FxPeriod,YcPeriod,CdrPeriod,ValuationPeriod,RcPeriod,Order,Year,Month
BOP,I,17,7,14,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,10,1900,1
MC,I,1,4,10,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,20,1900,1
RCU,I,4,4,8,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,EndOfPeriod,30,1900,1
CF,I,20,4,2,Average,NotApplicable,BeginningOfPeriod,Delta,EndOfPeriod,40,1900,1
IA,I,20,5,10,Average,BeginningOfPeriod,BeginningOfPeriod,Delta,EndOfPeriod,50,1900,1
AU,I,1,4,10,EndOfPeriod,BeginningOfPeriod,BeginningOfPeriod,EndOfPeriod,EndOfPeriod,60,1900,1
YCU,I,8,4,10,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,EndOfPeriod,70,1900,1
CRU,I,8,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,80,1900,1
EV,I,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,90,1900,1
BOP,N,1,4,10,Average,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,100,1900,1
MC,N,1,4,10,Average,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,105,1900,1
CF,N,4,4,2,Average,NotApplicable,EndOfPeriod,Delta,EndOfPeriod,110,1900,1
IA,N,4,4,10,Average,EndOfPeriod,EndOfPeriod,Delta,EndOfPeriod,120,1900,1
AU,N,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,130,1900,1
EV,N,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,140,1900,1
EV,C,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,175,1900,1
CL,C,2,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,180,1900,1
EA,C,4,4,8,EndOfPeriod,NotApplicable,NotApplicable,NotApplicable,EndOfPeriod,190,1900,1
CF,C,17,6,4,Average,NotApplicable,NotApplicable,NotApplicable,NotApplicable,193,1900,1
WO,C,17,2,4,Average,NotApplicable,NotApplicable,NotApplicable,NotApplicable,195,1900,1
AM,C,4,6,8,EndOfPeriod,NotApplicable,NotApplicable,NotApplicable,EndOfPeriod,200,1900,1
EOP,C,4,6,14,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,220,1900,1";


        await DataSource.DeleteAsync(DataSource.Query<AocConfiguration>());
        await Import.FromString(newAocConfig).WithType<AocConfiguration>().WithTarget(DataSource).ExecuteAsync();


        inputRawVariables = new RawVariable[]
        {
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "BOP",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "MC",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "CL",
                Novelty = "C", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "EV",
                Novelty = "C", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
        };


        var parentBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("EV", "C"), new AocStep[] {new AocStep("YCU", "I")}
            },
            {
                new AocStep("CL", "C"), new AocStep[] {new AocStep("EV", "C")}
            },
        };


        var referenceBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("BOP", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("CF", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("IA", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("EV", "C"), new AocStep[] {new AocStep("EV", "C")}
            },
            {
                new AocStep("CL", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("EA", "C"), new AocStep[] {new AocStep("CF", "C")}
            },

            {
                new AocStep("AM", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("EOP", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("CF", "C"), new AocStep[] {new AocStep("CF", "C")}
            },
            {
                new AocStep("WO", "C"), new AocStep[] {new AocStep("WO", "C")}
            },
        };


        var fullAocBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("CF", "I"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("MC", "I")}
            },
            {
                new AocStep("IA", "I"), new AocStep[]
                    {new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[]
                    {new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I")}
            },

            {
                new AocStep("EV", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"),
                }
            },
            {
                new AocStep("CL", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"),
                    new AocStep("EV", "C"),
                }
            },
            {
                new AocStep("EOP", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"),
                    new AocStep("EV", "C"), new AocStep("CL", "C"),
                }
            },
        };


        var activity = await CheckAocStepStructureAsync(inputRawVariables, Activity,
            parentBm, referenceBm, fullAocBm);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task SixthCheckAsync()
    {
        inputRawVariables = new RawVariable[]
        {
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "BOP",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "MC",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "BOP",
                Novelty = "N", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "AU",
                Novelty = "N", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "CL",
                Novelty = "C", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "EV",
                Novelty = "C", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
        };


        ParentBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("AU", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("EV", "C"), new AocStep[] {new AocStep("YCU", "I"), new AocStep("AU", "N")}
            },
            {
                new AocStep("CL", "C"), new AocStep[] {new AocStep("EV", "C")}
            },
        };


        var referenceBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("BOP", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("CF", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("IA", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("BOP", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("AU", "N"), new AocStep[] {new AocStep("AU", "N")}
            },
            {
                new AocStep("EV", "C"), new AocStep[] {new AocStep("EV", "C")}
            },
            {
                new AocStep("CL", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("EA", "C"), new AocStep[] {new AocStep("CF", "C")}
            },
            {
                new AocStep("AM", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("EOP", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("CF", "C"), new AocStep[] {new AocStep("CF", "C")}
            },
            {
                new AocStep("WO", "C"), new AocStep[] {new AocStep("WO", "C")}
            },
        };


        var fullAocBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("CF", "I"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("MC", "I")}
            },
            {
                new AocStep("IA", "I"), new AocStep[]
                    {new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I")}
            },
            {
                new AocStep("YCU", "I"), new AocStep[]
                    {new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I")}
            },

            {
                new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N"), new AocStep("CF", "N")}
            },
            {
                new AocStep("AU", "N"), new AocStep[]
                    {new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N")}
            },

            {
                new AocStep("EV", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"), new AocStep("AU", "N"),
                }
            },
            {
                new AocStep("CL", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"), new AocStep("AU", "N"),
                    new AocStep("EV", "C"),
                }
            },
            {
                new AocStep("EOP", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"), new AocStep("AU", "N"),
                    new AocStep("EV", "C"), new AocStep("CL", "C"),
                }
            },
        };


        var activity = await CheckAocStepStructureAsync(inputRawVariables, Activity,
            ParentBm, referenceBm, fullAocBm);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task SeventhCheckAsync()
    {
        await DataSource.DeleteAsync(DataSource.Query<AocConfiguration>());
        await Import.FromString(TestData.CanonicalAocConfig).WithType<AocConfiguration>().WithTarget(DataSource)
            .ExecuteAsync();


        var inputIfrsVariables = new IfrsVariable[]
        {
            new IfrsVariable
            {
                Partition = TestData.Partition.Id, Values = new double[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts,
                AocType = "BOP", Novelty = "C", AccidentYear = null, AmountType = "PR",
                EstimateType = "AA"
            },
            new IfrsVariable
            {
                Partition = TestData.Partition.Id, Values = new double[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts,
                AocType = "CF",
                Novelty = "C", AccidentYear = null, AmountType = "PR", EstimateType = "AA"
            },
            new IfrsVariable
            {
                Partition = TestData.Partition.Id, Values = new double[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts,
                AocType = "CF",
                Novelty = "C", AccidentYear = null, AmountType = "ACA", EstimateType = "A"
            },
            new IfrsVariable
            {
                Partition = TestData.Partition.Id, Values = new double[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts,
                AocType = "WO",
                Novelty = "C", AccidentYear = null, AmountType = "PR", EstimateType = "OA"
            },
        };


        ParentBm = null;


        ReferenceBm = null;


        FullAocBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("CF", "C"), new AocStep[] {new AocStep("BOP", "I"),}
            },
            {
                new AocStep("WO", "C"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("CF", "C"),}
            },
            {
                new AocStep("EOP", "C"), new AocStep[]
                    {new AocStep("BOP", "I"), new AocStep("CF", "C"), new AocStep("WO", "C")}
            },
        };


        var activity = await CheckAocStepStructureAsync(inputIfrsVariables, Activity,
            ParentBm, ReferenceBm, FullAocBm,
            StructureType.AocAccrual);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task EighthCheckAsync()
    {
        string newNovelties =
            @"@@Novelty
SystemName,DisplayName,Parent,Order
I,In Force,,1
N,New Business,,10
A,Aquisition,,15
C,Combined,,20";

        string newAocConfig =
            @"@@AocConfiguration,,,,,,,,,,,
AocType,Novelty,DataType,InputSource,StructureType,FxPeriod,YcPeriod,CdrPeriod,ValuationPeriod,RcPeriod,Order,Year,Month
BOP,I,17,7,14,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,10,1900,1
MC,I,1,4,10,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,20,1900,1
RCU,I,4,4,8,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,EndOfPeriod,30,1900,1
CF,I,20,4,2,Average,NotApplicable,BeginningOfPeriod,Delta,EndOfPeriod,40,1900,1
IA,I,20,5,10,Average,BeginningOfPeriod,BeginningOfPeriod,Delta,EndOfPeriod,50,1900,1
AU,I,1,4,10,EndOfPeriod,BeginningOfPeriod,BeginningOfPeriod,EndOfPeriod,EndOfPeriod,60,1900,1
YCU,I,8,4,10,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,EndOfPeriod,70,1900,1
CRU,I,8,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,80,1900,1
EV,I,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,90,1900,1

BOP,N,1,4,10,Average,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,100,1900,1
MC,N,1,4,10,Average,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,105,1900,1
CF,N,4,4,2,Average,NotApplicable,EndOfPeriod,Delta,EndOfPeriod,110,1900,1
IA,N,4,4,10,Average,EndOfPeriod,EndOfPeriod,Delta,EndOfPeriod,120,1900,1
AU,N,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,130,1900,1
EV,N,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,140,1900,1

BOP,A,1,4,10,Average,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,141,1900,1
MC,A,1,4,10,Average,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,142,1900,1
CF,A,4,4,2,Average,NotApplicable,EndOfPeriod,Delta,EndOfPeriod,143,1900,1
IA,A,4,4,10,Average,EndOfPeriod,EndOfPeriod,Delta,EndOfPeriod,144,1900,1
AU,A,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,145,1900,1
EV,A,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,146,1900,1

CL,C,2,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,180,1900,1
EA,C,4,4,8,EndOfPeriod,NotApplicable,NotApplicable,NotApplicable,EndOfPeriod,190,1900,1
CF,C,17,6,4,Average,NotApplicable,NotApplicable,NotApplicable,NotApplicable,193,1900,1
WO,C,17,2,4,Average,NotApplicable,NotApplicable,NotApplicable,NotApplicable,195,1900,1
AM,C,4,6,8,EndOfPeriod,NotApplicable,NotApplicable,NotApplicable,EndOfPeriod,200,1900,1
EOP,C,4,6,14,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,220,1900,1";


        await DataSource.DeleteAsync(DataSource.Query<Novelty>());
        await DataSource.DeleteAsync(DataSource.Query<AocConfiguration>());
        await Import.FromString(newNovelties).WithType<Novelty>().WithTarget(DataSource).ExecuteAsync();
        await Import.FromString(newAocConfig).WithType<AocConfiguration>().WithTarget(DataSource).ExecuteAsync();


        inputRawVariables = new RawVariable[]
        {
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "BOP",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "MC",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "BOP",
                Novelty = "N", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "EV",
                Novelty = "N", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "BOP",
                Novelty = "A", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "AU",
                Novelty = "A", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts, AocType = "CL",
                Novelty = "C", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
        };


        ParentBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}},
            {new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}},
            {new AocStep("EV", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("AU", "A"), new AocStep[] {new AocStep("BOP", "A")}},
            {
                new AocStep("CL", "C"),
                new AocStep[] {new AocStep("YCU", "I"), new AocStep("EV", "N"), new AocStep("AU", "A"),}
            },
        };


        ReferenceBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("BOP", "I"), new AocStep[] {new AocStep("BOP", "I")}},
            {new AocStep("MC", "I"), new AocStep[] {new AocStep("MC", "I")}},
            {new AocStep("CF", "I"), new AocStep[] {new AocStep("MC", "I")}},
            {new AocStep("IA", "I"), new AocStep[] {new AocStep("MC", "I")}},
            {new AocStep("YCU", "I"), new AocStep[] {new AocStep("MC", "I")}},
            {new AocStep("BOP", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("EV", "N"), new AocStep[] {new AocStep("EV", "N")}},
            {new AocStep("BOP", "A"), new AocStep[] {new AocStep("BOP", "A")}},
            {new AocStep("CF", "A"), new AocStep[] {new AocStep("BOP", "A")}},
            {new AocStep("IA", "A"), new AocStep[] {new AocStep("BOP", "A")}},
            {new AocStep("AU", "A"), new AocStep[] {new AocStep("AU", "A")}},
            {new AocStep("CL", "C"), new AocStep[] {new AocStep("CL", "C")}},
            {new AocStep("EA", "C"), new AocStep[] {new AocStep("CF", "C")}},
            {new AocStep("AM", "C"), new AocStep[] {new AocStep("CL", "C")}},
            {new AocStep("EOP", "C"), new AocStep[] {new AocStep("CL", "C")}},
            {new AocStep("CF", "C"), new AocStep[] {new AocStep("CF", "C")}},
            {new AocStep("WO", "C"), new AocStep[] {new AocStep("WO", "C")}},
        };


        FullAocBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}},
            {new AocStep("CF", "I"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("MC", "I")}},
            {
                new AocStep("IA", "I"),
                new AocStep[] {new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I")}
            },
            {
                new AocStep("YCU", "I"),
                new AocStep[]
                    {new AocStep("IA", "I"), new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I")}
            },
            {new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N"), new AocStep("CF", "N")}},
            {
                new AocStep("EV", "N"),
                new AocStep[] {new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N")}
            },
            {new AocStep("CF", "A"), new AocStep[] {new AocStep("BOP", "A")}},
            {new AocStep("IA", "A"), new AocStep[] {new AocStep("BOP", "A"), new AocStep("CF", "A")}},
            {
                new AocStep("AU", "A"),
                new AocStep[] {new AocStep("BOP", "A"), new AocStep("CF", "A"), new AocStep("IA", "A")}
            },
            {
                new AocStep("CL", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"), new AocStep("EV", "N"),
                    new AocStep("BOP", "A"), new AocStep("CF", "A"), new AocStep("IA", "A"), new AocStep("AU", "A"),
                }
            },
            {
                new AocStep("EOP", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("YCU", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"), new AocStep("EV", "N"),
                    new AocStep("BOP", "A"), new AocStep("CF", "A"), new AocStep("IA", "A"), new AocStep("AU", "A"),
                    new AocStep("CL", "C"),
                }
            },
        };


        var activity = await CheckAocStepStructureAsync(inputRawVariables, Activity,
            ParentBm, ReferenceBm, FullAocBm);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task NinthCheckAsync()
    {
        string newAocTypes =
            @"@@AocType
SystemName,DisplayName,Parent,Order
BOP,Opening Balance,,10
MC,Model Correction,,20
CF,Cash flow,,30
IA,Interest Accretion,,40
AU,Assumption Update,,50
YCU,Yield Curve Update,,60
EV,Experience Variance,,70
CL,Combined Liabilities,,80
AM,Amortization,,85
EOP,Closing Balance,,90";

        string newAocConfiguration =
            @"@@AocConfiguration,,,,,,,,,,,
AocType,Novelty,DataType,InputSource,StructureType,FxPeriod,YcPeriod,CdrPeriod,ValuationPeriod,RcPeriod,Order,Year,Month
BOP,I,17,7,14,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,10,1900,1
MC,I,1,4,10,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,20,1900,1
CF,I,20,4,2,Average,NotApplicable,BeginningOfPeriod,Delta,EndOfPeriod,40,1900,1
IA,I,20,5,10,Average,BeginningOfPeriod,BeginningOfPeriod,Delta,EndOfPeriod,50,1900,1
BOP,N,1,4,10,Average,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,100,1900,1
CF,N,4,4,2,Average,NotApplicable,EndOfPeriod,Delta,EndOfPeriod,110,1900,1
IA,N,4,4,10,Average,EndOfPeriod,EndOfPeriod,Delta,EndOfPeriod,120,1900,1
AU,C,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,130,1900,1
YCU,C,8,4,10,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,EndOfPeriod,135,2017,12
EV,C,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,140,1900,1
CL,C,2,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,180,1900,1
CF,C,17,6,4,Average,NotApplicable,NotApplicable,NotApplicable,NotApplicable,193,1900,1
AM,C,4,6,8,EndOfPeriod,NotApplicable,NotApplicable,NotApplicable,EndOfPeriod,200,1900,1
EOP,C,4,6,14,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,220,1900,1";


        await DataSource.DeleteAsync(DataSource.Query<AocType>());
        await DataSource.DeleteAsync(DataSource.Query<AocConfiguration>());
        await Import.FromString(newAocTypes).WithType<AocType>().WithTarget(DataSource).ExecuteAsync();
        await Import.FromString(newAocConfiguration).WithType<AocConfiguration>()
            .WithTarget(DataSource).ExecuteAsync();


        var inputVariables = new RawVariable[]
        {
            new RawVariable
            {
                AocType = "BOP", Novelty = "N", AccidentYear = null, AmountType = "CL",
                EstimateType = "BE",
                Partition = TestData.Partition.Id, Values = new double[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts
            },
            new RawVariable
            {
                AocType = "EV", Novelty = "C", AccidentYear = null, AmountType = "CL", EstimateType = "BE",
                Partition = TestData.Partition.Id, Values = new double[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts
            },
            new RawVariable
            {
                AocType = "CL", Novelty = "C", AccidentYear = null, AmountType = "CL", EstimateType = "BE",
                Partition = TestData.Partition.Id, Values = new double[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts
            },
        };


        ParentBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("YCU", "C"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("BOP", "N")}},
            {new AocStep("EV", "C"), new AocStep[] {new AocStep("YCU", "C")}},
            {new AocStep("CL", "C"), new AocStep[] {new AocStep("EV", "C"),}},
        };

        ReferenceBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("BOP", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("YCU", "C"), new AocStep[] {new AocStep("BOP", "N"), new AocStep("BOP", "I")}},
            {new AocStep("EV", "C"), new AocStep[] {new AocStep("EV", "C")}},
            {new AocStep("CL", "C"), new AocStep[] {new AocStep("CL", "C")}},
            {new AocStep("AM", "C"), new AocStep[] {new AocStep("CL", "C")}},
            {new AocStep("EOP", "C"), new AocStep[] {new AocStep("CL", "C")}},
            {new AocStep("CF", "C"), new AocStep[] {new AocStep("CF", "C")}},
            {new AocStep("CF", "I"), new AocStep[] {new AocStep("BOP", "I")}},
            {new AocStep("IA", "I"), new AocStep[] {new AocStep("BOP", "I")}},
            {new AocStep("BOP", "I"), new AocStep[] {new AocStep("BOP", "I")}},
        };

        FullAocBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("CF", "I"), new AocStep[] {new AocStep("BOP", "I"),}},
            {new AocStep("IA", "I"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("CF", "I")}},
            {new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N"), new AocStep("CF", "N")}},

            {
                new AocStep("YCU", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"),
                }
            },
            {
                new AocStep("EV", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"),
                    new AocStep("YCU", "C"),
                }
            },
            {
                new AocStep("CL", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"),
                    new AocStep("YCU", "C"), new AocStep("EV", "C")
                }
            },
            {
                new AocStep("EOP", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"),
                    new AocStep("YCU", "C"), new AocStep("EV", "C"), new AocStep("CL", "C"),
                }
            },
        };


        var activity = await CheckAocStepStructureAsync(inputVariables, Activity,
            ParentBm, ReferenceBm, FullAocBm);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }


    [Fact]
    public async Task TenthCheckAsync()
    {
        var inputVariables = new RawVariable[]
        {
            new RawVariable
            {
                AocType = "BOP", Novelty = "N", AccidentYear = null, AmountType = "CL", EstimateType = "BE",
                Partition = TestData.Partition.Id, Values = new double[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts
            },
            new RawVariable
            {
                AocType = "AU", Novelty = "C", AccidentYear = null, AmountType = "CL", EstimateType = "BE",
                Partition = TestData.Partition.Id, Values = new double[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts
            },
            new RawVariable
            {
                AocType = "EV", Novelty = "C", AccidentYear = null, AmountType = "CL", EstimateType = "BE",
                Partition = TestData.Partition.Id, Values = new double[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts
            },
            new RawVariable
            {
                AocType = "CL", Novelty = "C", AccidentYear = null, AmountType = "CL", EstimateType = "BE",
                Partition = TestData.Partition.Id, Values = new double[] {1.0},
                DataNode = TestData.GroupOfInsuranceContracts
            },
        };


        ParentBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("AU", "C"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("BOP", "N")}},
            {new AocStep("YCU", "C"), new AocStep[] {new AocStep("AU", "C"),}},
            {new AocStep("EV", "C"), new AocStep[] {new AocStep("YCU", "C")}},
            {new AocStep("CL", "C"), new AocStep[] {new AocStep("EV", "C"),}},
        };

        ReferenceBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("BOP", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N")}},

            {new AocStep("AU", "C"), new AocStep[] {new AocStep("AU", "C")}},
            {new AocStep("YCU", "C"), new AocStep[] {new AocStep("AU", "C")}},
            {new AocStep("EV", "C"), new AocStep[] {new AocStep("EV", "C")}},
            {new AocStep("CL", "C"), new AocStep[] {new AocStep("CL", "C")}},
            {new AocStep("AM", "C"), new AocStep[] {new AocStep("CL", "C")}},
            {new AocStep("EOP", "C"), new AocStep[] {new AocStep("CL", "C")}},
            {new AocStep("CF", "C"), new AocStep[] {new AocStep("CF", "C")}},
            {new AocStep("CF", "I"), new AocStep[] {new AocStep("BOP", "I")}},
            {new AocStep("IA", "I"), new AocStep[] {new AocStep("BOP", "I")}},
            {new AocStep("BOP", "I"), new AocStep[] {new AocStep("BOP", "I")}},
        };

        FullAocBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("CF", "I"), new AocStep[] {new AocStep("BOP", "I"),}},
            {new AocStep("IA", "I"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("CF", "I")}},
            {new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}},
            {new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N"), new AocStep("CF", "N")}},

            {
                new AocStep("AU", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"),
                }
            },
            {
                new AocStep("YCU", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"),
                    new AocStep("AU", "C"),
                }
            },
            {
                new AocStep("EV", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"),
                    new AocStep("AU", "C"), new AocStep("YCU", "C"),
                }
            },
            {
                new AocStep("CL", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"),
                    new AocStep("AU", "C"), new AocStep("YCU", "C"), new AocStep("EV", "C")
                }
            },
            {
                new AocStep("EOP", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"),
                    new AocStep("AU", "C"), new AocStep("YCU", "C"), new AocStep("EV", "C"), new AocStep("CL", "C"),
                }
            },
        };


        var activity = await CheckAocStepStructureAsync(inputVariables, Activity,
            ParentBm, ReferenceBm, FullAocBm);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Check11Async()
    {
        string newAocTypes =
            @"@@AocType
SystemName,DisplayName,Parent,Order
BOP,Opening Balance,,10
MC,Model Correction,,20
CF,Cash flow,,30
IA,Interest Accretion,,40
CRU,Credit Risk Update,,45
AU,Assumption Update,,50
YCU,Yield Curve Update,,60
EV,Experience Variance,,70
CL,Combined Liabilities,,80
EA,Experience Adjustment,,81,
AM,Amortization,,85
EOP,Closing Balance,,90";

        string newAocConfiguration =
            @"@@AocConfiguration,,,,,,,,,,,
AocType,Novelty,DataType,InputSource,StructureType,FxPeriod,YcPeriod,CdrPeriod,ValuationPeriod,RcPeriod,Order,Year,Month
BOP,I,17,7,14,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,10,1900,1
MC,I,1,4,10,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,20,1900,1
CF,I,20,4,2,Average,NotApplicable,BeginningOfPeriod,Delta,EndOfPeriod,40,1900,1
IA,I,20,5,10,Average,BeginningOfPeriod,BeginningOfPeriod,Delta,EndOfPeriod,50,1900,1
CRU,I,8,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,80,1900,1
BOP,N,1,4,10,Average,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,100,1900,1
CF,N,4,4,2,Average,NotApplicable,EndOfPeriod,Delta,EndOfPeriod,110,1900,1
IA,N,4,4,10,Average,EndOfPeriod,EndOfPeriod,Delta,EndOfPeriod,120,1900,1
AU,C,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,130,1900,1
YCU,C,8,4,10,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,EndOfPeriod,135,2017,12
EV,C,1,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,140,1900,1
CL,C,2,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,180,1900,1
EA,C,4,4,8,EndOfPeriod,NotApplicable,NotApplicable,NotApplicable,EndOfPeriod,190,1900,1
CF,C,17,6,4,Average,NotApplicable,NotApplicable,NotApplicable,NotApplicable,193,1900,1
AM,C,4,6,8,EndOfPeriod,NotApplicable,NotApplicable,NotApplicable,EndOfPeriod,200,1900,1
EOP,C,4,6,14,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,220,1900,1";


        await DataSource.DeleteAsync(DataSource.Query<AocType>());
        await DataSource.DeleteAsync(DataSource.Query<AocConfiguration>());
        await Import.FromString(newAocTypes).WithType<AocType>().WithTarget(DataSource).ExecuteAsync();
        await Import.FromString(newAocConfiguration).WithType<AocConfiguration>()
            .WithTarget(DataSource)
            .ExecuteAsync();


        inputRawVariables = new RawVariable[]
        {
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfReinsuranceContracts, AocType = "BOP",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfReinsuranceContracts, AocType = "MC",
                Novelty = "I", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfReinsuranceContracts, AocType = "BOP",
                Novelty = "N", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
            new RawVariable
            {
                Partition = TestData.Partition.Id, Values = new[] {1.0},
                DataNode = TestData.GroupOfReinsuranceContracts, AocType = "CL",
                Novelty = "C", AccidentYear = null, AmountType = "PR", EstimateType = "BE"
            },
        };


        ParentBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("CRU", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("YCU", "C"), new AocStep[] {new AocStep("MC", "I"), new AocStep("BOP", "N"),}
            },
            {
                new AocStep("CL", "C"), new AocStep[] {new AocStep("YCU", "C"),}
            },
        };

        var parentBmCdr = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}},
            {new AocStep("CRU", "I"), new AocStep[] {new AocStep("MC", "I")}},
            {new AocStep("YCU", "C"), new AocStep[] {new AocStep("CRU", "I"), new AocStep("BOP", "N")}},
            {new AocStep("CL", "C"), new AocStep[] {new AocStep("YCU", "C"),}},
        };

        ReferenceBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("BOP", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("CF", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("IA", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("CRU", "I"), new AocStep[] {new AocStep("MC", "I")}
            },
            {
                new AocStep("BOP", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("YCU", "C"), new AocStep[] {new AocStep("MC", "I"), new AocStep("BOP", "N")}
            },
            {
                new AocStep("CL", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("EA", "C"), new AocStep[] {new AocStep("CF", "C")}
            },
            {
                new AocStep("AM", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("EOP", "C"), new AocStep[] {new AocStep("CL", "C")}
            },
            {
                new AocStep("CF", "C"), new AocStep[] {new AocStep("CF", "C")}
            },
        };
        FullAocBm = new Dictionary<AocStep, IEnumerable<AocStep>>()
        {
            {
                new AocStep("MC", "I"), new AocStep[] {new AocStep("BOP", "I")}
            },
            {
                new AocStep("CF", "I"), new AocStep[] {new AocStep("BOP", "I"), new AocStep("MC", "I")}
            },
            {
                new AocStep("IA", "I"), new AocStep[]
                    {new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I")}
            },
            {
                new AocStep("CRU", "I"), new AocStep[]
                    {new AocStep("IA", "I"), new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I")}
            },
            {
                new AocStep("CF", "N"), new AocStep[] {new AocStep("BOP", "N")}
            },
            {
                new AocStep("IA", "N"), new AocStep[] {new AocStep("BOP", "N"), new AocStep("CF", "N")}
            },

            {
                new AocStep("YCU", "C"), new AocStep[]
                {
                    new AocStep("IA", "I"), new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"),
                    new AocStep("CRU", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"),
                }
            },
            {
                new AocStep("CL", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("CRU", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"),
                    new AocStep("YCU", "C"),
                }
            },
            {
                new AocStep("EOP", "C"), new AocStep[]
                {
                    new AocStep("BOP", "I"), new AocStep("MC", "I"), new AocStep("CF", "I"), new AocStep("IA", "I"),
                    new AocStep("CRU", "I"),
                    new AocStep("BOP", "N"), new AocStep("CF", "N"), new AocStep("IA", "N"),
                    new AocStep("YCU", "C"), new AocStep("CL", "C"),
                }
            },
        };


        var activity = await CheckAocStepStructureAsync(inputRawVariables, Activity,
            ParentBm, ReferenceBm, FullAocBm,
            StructureType.AocPresentValue, parentBmCdr);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }
}