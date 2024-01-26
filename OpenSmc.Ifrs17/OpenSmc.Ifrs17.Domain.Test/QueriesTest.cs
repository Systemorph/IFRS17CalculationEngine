using FluentAssertions;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Import;
using Systemorph.Vertex.Scopes.Proxy;
using Systemorph.Vertex.Workspace;


namespace OpenSmc.Ifrs17.Domain.Tests;

public class QueriesTest : TestBase
{
    private readonly string gic;
    private readonly string scenario;
    private readonly string gic2;
    private readonly string xgic;
    private readonly string gric1;

    public QueriesTest(IImportVariable import, IDataSource dataSource,
        IWorkspaceVariable work, IActivityVariable activity, IScopeFactory scopes) :
        base(import, dataSource, work, activity, scopes)
    {
        gic = "DT1.1";
        scenario = "MTUP";
        Work.Reset(x => x.ResetInitializationRules().ResetCurrentPartitions());
        gic2 = "gic2";
        xgic = "xgic";
        gric1 = "gric1";
    }


    public async Task<ActivityLog> CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(Args args,
        IEnumerable<YieldCurve> testData,
        (int Year, int Month, string Scenario) expectedCurrentPeriod,
        (int Year, int Month, string Scenario) expectedPreviousPeriod)
    {
        Activity.Start();
        await Work.UpdateAsync(testData);
        var eurCurrentAndPreviousYieldCurve =
            (await Work.LoadCurrentAndPreviousParameterAsync<YieldCurve>(args, x => x.Currency))["EUR"];

        //Check Current Period
        eurCurrentAndPreviousYieldCurve[Consts.CurrentPeriod].Year.Should().Be(expectedCurrentPeriod.Year);
        eurCurrentAndPreviousYieldCurve[Consts.CurrentPeriod].Month.Should().Be(expectedCurrentPeriod.Month);
        eurCurrentAndPreviousYieldCurve[Consts.CurrentPeriod].Scenario.Should().Be(expectedCurrentPeriod.Scenario);

        //Check Previous Period
        eurCurrentAndPreviousYieldCurve[Consts.PreviousPeriod].Year.Should().Be(expectedPreviousPeriod.Year);
        eurCurrentAndPreviousYieldCurve[Consts.PreviousPeriod].Month.Should().Be(expectedPreviousPeriod.Month);
        eurCurrentAndPreviousYieldCurve[Consts.PreviousPeriod].Scenario.Should().Be(expectedPreviousPeriod.Scenario);

        await Work.DeleteAsync(Work.Query<YieldCurve>().ToArray());
        return Activity.Finish();
    }

    private bool Equal(double? a, double? b) => (a == null && b == null) ||
                                                (a != null && b != null && Math.Abs((double) a - (double) b) < 1e-8);

    [Fact]
    public async Task Test1Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);
        var testData = new YieldCurve {Currency = "EUR", Year = 2020, Month = 9};

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData.RepeatOnce(), (2020, 9, null),
                (2020, 9, null));

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test2Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new[]
        {
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 9, Scenario = scenario},
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 9}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2020, 9, scenario),
                (2020, 9, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test3Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);
        var testData = new YieldCurve {Currency = "EUR", Year = 2019, Month = 12, Scenario = null};

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData.RepeatOnce(), (2019, 12, null),
                (2019, 12, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test4Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 9, Scenario = scenario},
            new YieldCurve {Currency = "EUR", Year = 2019, Month = 12, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2020, 9, scenario),
                (2019, 12, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test5Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 6, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 3, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2020, 6, null),
                (2020, 6, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test6Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 6, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 3, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2020, 6, null),
                (2020, 6, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test7Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 6, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2019, Month = 3, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2020, 6, null),
                (2019, 3, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test8Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 9, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 6, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2020, 9, null),
                (2020, 9, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test9Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2019, Month = 9, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 9, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2020, 9, null),
                (2019, 9, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test10Async()
    {
        var args = new Args("EUR", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 9, Scenario = scenario},
            new YieldCurve {Currency = "EUR", Year = 2019, Month = 12, Scenario = scenario},
            new YieldCurve {Currency = "EUR", Year = 2019, Month = 12, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2020, 9, scenario),
                (2019, 12, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test11Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2019, Month = 12, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2019, Month = 9, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2019, 12, null),
                (2019, 12, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test12Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2019, Month = 12, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2018, Month = 9, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2019, 12, null),
                (2019, 12, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test13Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2019, Month = 6, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2016, Month = 9, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2016, Month = 6, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2019, 6, null),
                (2019, 6, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test14Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2019, Month = 6, Scenario = scenario},
            new YieldCurve {Currency = "EUR", Year = 2019, Month = 6, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2016, Month = 9, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2016, Month = 6, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2019, 6, null),
                (2019, 6, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test15Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 6, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 3, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2015, Month = 9, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2016, Month = 3, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2020, 6, null),
                (2016, 3, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test16Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 6, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2020, Month = 3, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2021, Month = 9, Scenario = null},
            new YieldCurve {Currency = "EUR", Year = 2016, Month = 3, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentAndPreviousParameterForYieldCurveAsync(args, testData, (2020, 6, null),
                (2016, 3, null));


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }


    public async Task<ActivityLog> CheckLoadDataNodeStateAsync(Args args, IEnumerable<DataNodeState> testData,
        bool isExpectedToBeActive)
    {
        Activity.Start();
        await Work.Partition.SetAsync<PartitionByReportingNode>(args);
        await Work.UpdateAsync(testData);
        var isActive = (await Work.LoadDataNodeStateAsync(args)).Keys.Contains(gic);

        isActive.Should().Be(isExpectedToBeActive);

        Work.Reset(x => x.ResetCurrentPartitions());
        return Activity.Finish();
    }

    [Fact]
    public async Task Test17Async()
    {
        var args = new Args("CH", 2020, 12, Periodicity.Monthly, null);

        var testData = new DataNodeState[]
        {
            new DataNodeState {DataNode = gic, Year = 2019, Month = 12, State = State.Active},
            new DataNodeState {DataNode = gic, Year = 2020, Month = 6, State = State.Inactive}
        };

        var activity = await CheckLoadDataNodeStateAsync(args, testData, false);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test18Async()
    {
        var args = new Args("CH", 2020, 12, Periodicity.Monthly, null);

        var testData = new DataNodeState[]
        {
            new DataNodeState {DataNode = gic, Year = 2020, Month = 3, State = State.Active},
            new DataNodeState {DataNode = gic, Year = 2020, Month = 9, State = State.Inactive}
        };

        var activity = await CheckLoadDataNodeStateAsync(args, testData, false);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }


// Modify this method to reflect to errors logged at the level of query
    public async Task<ActivityLog> CheckLoadYieldCurveAsync(Args args, IEnumerable<YieldCurve> testData,
        (int year, int month, string yieldCurveName, string valuationApproach) dataNodeTestData,
        double? expectedCurrentPreviousFirstYcValue = null,
        double? expectedCurrentFirstYcValue = null,
        double? expectedLockedFirstYcValue = null)
    {
        Activity.Start();
        await Work.Partition.SetAsync<PartitionByReportingNode>(args);
        await Work.UpdateAsync(testData);
        await Work.UpdateAsync<ReportingNode>(new ReportingNode[]
            {new ReportingNode {Currency = "EUR", SystemName = "CH"}});

        var dataNodes = new DataNodeData[]
        {
            new DataNodeData
            {
                ValuationApproach = dataNodeTestData.valuationApproach, Year = dataNodeTestData.year,
                Month = dataNodeTestData.month, DataNode = gic, ContractualCurrency = "EUR",
                YieldCurveName = dataNodeTestData.yieldCurveName
            }
        };
        var eurLockedYieldCurve = await Work.LoadLockedInYieldCurveAsync(args, dataNodes);
        var eurCurrentYieldCurve = await Work.LoadCurrentYieldCurveAsync(args, dataNodes);

        Work.Reset(x => x.ResetCurrentPartitions());

        var errors = new List<string>();
        if (eurCurrentYieldCurve[gic] == null)
            return Activity.Finish();
        double? currentPrevPeriod =
            eurCurrentYieldCurve.TryGetValue(gic, out var ycByPeriod) && ycByPeriod != null &&
            ycByPeriod.TryGetValue(Consts.PreviousPeriod, out var ycP)
                ? ycP?.Values.FirstOrDefault()
                : null;
        if (!Equal(currentPrevPeriod, expectedCurrentPreviousFirstYcValue))
            errors.Add(
                $"Current YieldCurve for Previous Period not matching: Expected {expectedCurrentPreviousFirstYcValue} \n Computed {currentPrevPeriod}.");

        double? currentCurrPeriod =
            eurCurrentYieldCurve.TryGetValue(gic, out ycByPeriod) && ycByPeriod != null &&
            ycByPeriod.TryGetValue(Consts.CurrentPeriod, out var ycC)
                ? ycC?.Values.FirstOrDefault()
                : null;
        if (!Equal(currentCurrPeriod, expectedCurrentFirstYcValue))
            errors.Add(
                $"Current YieldCurve for Current Period not matching: Expected {expectedCurrentFirstYcValue} Computed {currentCurrPeriod}.");

        if (dataNodeTestData.valuationApproach == ValuationApproaches.BBA)
        {
            double? lockedYc = eurLockedYieldCurve.TryGetValue(gic, out var yc) ? yc?.Values.FirstOrDefault() : null;
            if (!Equal(lockedYc, expectedLockedFirstYcValue))
                errors.Add(
                    $"LockedIn YieldCurve not matching: Expected {expectedLockedFirstYcValue}  Computed {lockedYc}.");
        }

        if (errors.Any()) ApplicationMessage.Log(Error.Generic, string.Join("\n", errors));
        return Activity.Finish();
    }

    [Fact]
    public async Task Test19Async()
    {
        var args = new Args("CH", 2020, 12, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 12, Name = "A",
                Values = new double[] {7.1, 7.2, 7.3, 7.4, 7.5, 7.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 12, Values = new double[] {0.1, 0.2, 0.3, 0.4, 0.5, 0.6}},
        };

        (int year, int month, string yieldCurveName, string valuationApproach)
            dataNodeTestData = (2020, 12, "A", "BBA");
        var activity = await CheckLoadYieldCurveAsync(args, testData, dataNodeTestData, 0.1, 0.1, 7.1);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test20Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 12, Name = "A",
                Values = new double[] {7.1, 7.2, 7.3, 7.4, 7.5, 7.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 12, Values = new double[] {0.1, 0.2, 0.3, 0.4, 0.5, 0.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 9, Name = "A",
                Values = new double[] {6.1, 6.2, 6.3, 6.4, 6.5, 6.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 9, Values = new double[] {0.01, 0.02, 0.03, 0.04, 0.05, 0.06}},
        };

        (int year, int month, string yieldCurveName, string valuationApproach) dataNodeTestData = (2020, 1, "A", "BBA");
        var activity = await CheckLoadYieldCurveAsync(args, testData, dataNodeTestData, 0.01, 0.01, 6.1);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test21Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 6, Values = new double[] {0.1, 0.2, 0.3, 0.4, 0.5, 0.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "A",
                Values = new double[] {7.1, 7.2, 7.3, 7.4, 7.5, 7.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 3, Values = new double[] {2.1, 2.2, 2.3, 2.4, 2.5, 2.6}},
            new YieldCurve
                {Currency = "EUR", Year = 2021, Month = 9, Values = new double[] {4.1, 4.2, 4.3, 4.4, 4.5, 4.6}},
            new YieldCurve
                {Currency = "EUR", Year = 2016, Month = 3, Values = new double[] {6.1, 6.2, 6.3, 6.4, 6.5, 6.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "A",
                Values = new double[] {9.1, 9.2, 9.3, 9.4, 9.5, 9.6}
            }
        };

        (int year, int month, string yieldCurveName, string valuationApproach)
            dataNodeTestData = (2016, 6, null, "BBA");
        var activity = await CheckLoadYieldCurveAsync(args, testData, dataNodeTestData, 6.1, 0.1, 6.1);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test22Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "A",
                Values = new double[] {0.1, 0.2, 0.3, 0.4, 0.5, 0.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 6, Values = new double[] {1.1, 1.2, 1.3, 1.4, 1.5, 1.6}},
            new YieldCurve
            {
                Currency = "CHF", Year = 2020, Month = 6, Name = "A",
                Values = new double[] {3.1, 3.2, 3.3, 3.4, 3.5, 3.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "B",
                Values = new double[] {2.1, 2.2, 2.3, 2.4, 2.5, 2.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "A",
                Values = new double[] {6.1, 6.2, 6.3, 6.4, 6.5, 6.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2016, Month = 3, Values = new double[] {9.1, 9.2, 9.3, 9.4, 9.5, 9.6}},
            new YieldCurve
            {
                Currency = "CHF", Year = 2016, Month = 3, Name = "A",
                Values = new double[] {8.1, 8.2, 8.3, 8.4, 8.5, 8.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "B",
                Values = new double[] {7.1, 7.2, 7.3, 7.4, 7.5, 7.6}
            }
        };

        (int year, int month, string yieldCurveName, string valuationApproach) dataNodeTestData = (2016, 6, "A", "BBA");
        var activity = await CheckLoadYieldCurveAsync(args, testData, dataNodeTestData, 9.1, 1.1, 6.1);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test23Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 12, Name = "A",
                Values = new double[] {11.1, 11.2, 11.3, 11.4, 11.5, 11.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 12,
                Values = new double[] {100.1, 100.2, 100.3, 100.4, 100.5, 100.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 9, Name = "A",
                Values = new double[] {12.1, 12.2, 12.3, 12.4, 12.5, 12.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 9, Values = new double[] {90.1, 90.2, 90.3, 90.4, 90.5, 90.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "A",
                Values = new double[] {13.1, 13.2, 13.3, 13.4, 13.5, 13.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 6, Values = new double[] {80.1, 80.2, 80.3, 80.4, 80.5, 80.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 3, Name = "A",
                Values = new double[] {1.1, 1.2, 1.3, 1.4, 1.5, 1.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 3, Values = new double[] {70.1, 70.2, 70.3, 70.4, 70.5, 70.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2019, Month = 12, Name = "A",
                Values = new double[] {9.1, 9.2, 9.3, 9.4, 9.5, 9.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2019, Month = 12, Values = new double[] {60.1, 60.2, 60.3, 60.4, 60.5, 60.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2019, Month = 9, Name = "A",
                Values = new double[] {10.1, 10.2, 10.3, 10.4, 10.5, 10.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2019, Month = 9, Values = new double[] {50.1, 50.2, 50.3, 50.4, 50.5, 50.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2019, Month = 6, Name = "A",
                Values = new double[] {2.1, 2.2, 2.3, 2.4, 2.5, 2.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2019, Month = 6, Values = new double[] {40.1, 40.2, 40.3, 40.4, 40.5, 40.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2019, Month = 3, Name = "A",
                Values = new double[] {7.1, 7.2, 7.3, 7.4, 7.5, 7.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2019, Month = 3, Values = new double[] {30.1, 30.2, 30.3, 30.4, 30.5, 30.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2018, Month = 6, Name = "A",
                Values = new double[] {3.1, 3.2, 3.3, 3.4, 3.5, 3.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2018, Month = 6, Values = new double[] {20.1, 20.2, 20.3, 20.4, 20.5, 20.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2017, Month = 6, Name = "A",
                Values = new double[] {4.1, 4.2, 4.3, 4.4, 4.5, 4.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 12, Name = "A",
                Values = new double[] {8.1, 8.2, 8.3, 8.4, 8.5, 8.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "A",
                Values = new double[] {6.1, 6.2, 6.3, 6.4, 6.5, 6.6}
            }
        };

        (int year, int month, string yieldCurveName, string valuationApproach) dataNodeTestData = (2016, 6, "A", "BBA");
        var activity = await CheckLoadYieldCurveAsync(args, testData, dataNodeTestData, 60.1, 90.1, 8.1);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test24Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "A",
                Values = new double[] {0.1, 0.2, 0.3, 0.4, 0.5, 0.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "B",
                Values = new double[] {2.1, 2.2, 2.3, 2.4, 2.5, 2.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "A",
                Values = new double[] {6.1, 6.2, 6.3, 6.4, 6.5, 6.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "B",
                Values = new double[] {4.1, 4.2, 4.3, 4.4, 4.5, 4.6}
            }
        };

        (int year, int month, string yieldCurveName, string valuationApproach) dataNodeTestData = (2016, 6, "A", "VFA");
        var activity = await CheckLoadYieldCurveAsync(args, testData, dataNodeTestData, 6.1, 0.1);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test25Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "B",
                Values = new double[] {0.1, 0.2, 0.3, 0.4, 0.5, 0.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 6, Values = new double[] {10.1, 10.2, 10.3, 10.4, 10.5, 10.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "B",
                Values = new double[] {2.1, 2.2, 2.3, 2.4, 2.5, 2.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "C",
                Values = new double[] {4.1, 4.2, 4.3, 4.4, 4.5, 4.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "B",
                Values = new double[] {6.1, 6.2, 6.3, 6.4, 6.5, 6.6}
            }
        };

        (int year, int month, string yieldCurveName, string valuationApproach) dataNodeTestData = (2016, 2, "A", "BBA");
        var activity = await CheckLoadYieldCurveAsync(args, testData, dataNodeTestData, 10.1, 10.1);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test26Async()
    {
        var args = new Args("CH", 2016, 9, Periodicity.Monthly, null);

        var testData = new YieldCurve[]
        {
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "B",
                Values = new double[] {0.1, 0.2, 0.3, 0.4, 0.5, 0.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2016, Month = 6, Values = new double[] {10.1, 10.2, 10.3, 10.4, 10.5, 10.6}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "B",
                Values = new double[] {2.1, 2.2, 2.3, 2.4, 2.5, 2.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "C",
                Values = new double[] {4.1, 4.2, 4.3, 4.4, 4.5, 4.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "B",
                Values = new double[] {6.1, 6.2, 6.3, 6.4, 6.5, 6.6}
            }
        };

        (int year, int month, string yieldCurveName, string valuationApproach) dataNodeTestData = (2016, 2, "A", "BBA");
        var activity = await CheckLoadYieldCurveAsync(args, testData, dataNodeTestData, 10.1, 10.1);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test27Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, "YCUP");

        var testData = new YieldCurve[]
        {
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "A",
                Values = new double[] {0.1, 0.2, 0.3, 0.4, 0.5, 0.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 6, Values = new double[] {1.1, 1.2, 1.3, 1.4, 1.5, 1.6}},
            new YieldCurve
            {
                Currency = "CHF", Year = 2020, Month = 6, Name = "A",
                Values = new double[] {3.1, 3.2, 3.3, 3.4, 3.5, 3.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "B",
                Values = new double[] {2.1, 2.2, 2.3, 2.4, 2.5, 2.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "A",
                Values = new double[] {6.1, 6.2, 6.3, 6.4, 6.5, 6.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2016, Month = 3, Values = new double[] {9.1, 9.2, 9.3, 9.4, 9.5, 9.6}},
            new YieldCurve
            {
                Currency = "CHF", Year = 2016, Month = 3, Name = "A",
                Values = new double[] {8.1, 8.2, 8.3, 8.4, 8.5, 8.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "B",
                Values = new double[] {7.1, 7.2, 7.3, 7.4, 7.5, 7.6}
            }
        };

        (int year, int month, string yieldCurveName, string valuationApproach) dataNodeTestData = (2016, 6, "A", "BBA");
        var activity = await CheckLoadYieldCurveAsync(args, testData, dataNodeTestData, 9.1, 1.1, 6.1);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test28Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, "YCUP");

        var testData = new YieldCurve[]
        {
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "A",
                Values = new double[] {0.1, 0.2, 0.3, 0.4, 0.5, 0.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 6, Values = new double[] {1.1, 1.2, 1.3, 1.4, 1.5, 1.6}},
            new YieldCurve
            {
                Currency = "CHF", Year = 2020, Month = 6, Name = "A",
                Values = new double[] {3.1, 3.2, 3.3, 3.4, 3.5, 3.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2020, Month = 6, Name = "B",
                Values = new double[] {2.1, 2.2, 2.3, 2.4, 2.5, 2.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "A",
                Values = new double[] {6.1, 6.2, 6.3, 6.4, 6.5, 6.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2016, Month = 3, Values = new double[] {9.1, 9.2, 9.3, 9.4, 9.5, 9.6}},
            new YieldCurve
            {
                Currency = "CHF", Year = 2016, Month = 3, Name = "A",
                Values = new double[] {8.1, 8.2, 8.3, 8.4, 8.5, 8.6}
            },
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "B",
                Values = new double[] {7.1, 7.2, 7.3, 7.4, 7.5, 7.6}
            },
            new YieldCurve
                {Currency = "EUR", Year = 2016, Month = 3, Scenario = "YCUP", Values = new double[] {90.1, 9.2, 9.3}},
            new YieldCurve
                {Currency = "EUR", Year = 2020, Month = 6, Scenario = "YCUP", Values = new double[] {10.1, 1.2, 1.3}},
            new YieldCurve
            {
                Currency = "EUR", Year = 2016, Month = 3, Name = "A", Scenario = "YCUP",
                Values = new double[] {60.1, 6.2, 6.3}
            },
        };

        (int year, int month, string yieldCurveName, string valuationApproach) dataNodeTestData = (2016, 6, "A", "BBA");
        var activity = await CheckLoadYieldCurveAsync(args, testData, dataNodeTestData, 9.1, 10.1, 60.1);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }


    public async Task<ActivityLog> CheckLoadInterDataNodeParameterAsync(Args args,
        IEnumerable<InterDataNodeParameter> testData,
        int previousYear,
        int? currentYear = null)
    {
        Activity.Start();
        currentYear = currentYear ?? previousYear;
        await Work.Partition.SetAsync<PartitionByReportingNode>(args);
        await Work.UpdateAsync(testData);

        var interDataNodeParameters = await Queries.LoadInterDataNodeParametersAsync(Work, args);

        //Check Keys
        var uniqueLinks = testData.Select(x => x.DataNode).Concat(testData.Select(x => x.LinkedDataNode)).ToHashSet();
        uniqueLinks.Intersect(interDataNodeParameters.Keys).Count().Should().Be(uniqueLinks.Count());

        //CheckInnerDictionaries
        var currentYearData = testData.Where(x => x.Year == currentYear);
        var previousYearData = testData.Where(x => x.Year == previousYear);

        var errors = new List<string>();
        foreach (var dn in uniqueLinks)
        {
            //Set up BM counts
            var currentLinks = currentYearData.Select(x => x.DataNode)
                .Concat(currentYearData.Select(x => x.LinkedDataNode));
            var currentLinksCount = currentLinks.Where(x => x == dn).Count();
            var prevlinks = previousYearData.Select(x => x.DataNode)
                .Concat(previousYearData.Select(x => x.LinkedDataNode));
            var previousLinksCount = prevlinks.Where(x => x == dn).Count();

            //Current Period
            var computedCount = interDataNodeParameters[dn][Consts.CurrentPeriod].Count();

            if (currentYearData.Any())
            {
                if (computedCount != currentLinksCount)
                    errors.Add(
                        $"Current DataNode parameter count for {dn} not matching:\n Expected {currentLinksCount} \n Computed {computedCount}.");
            }
            else if (computedCount != previousLinksCount)
                errors.Add(
                    $"Current DataNode parameter count for {dn} not matching:\n Expected {previousLinksCount} \n Computed {computedCount}.");

            foreach (var param in interDataNodeParameters[dn][Consts.CurrentPeriod])
            {
                var linkedDn = param.DataNode == dn ? param.LinkedDataNode : param.DataNode;
                var isLinkInPrevPeriod = previousYearData.Any(x =>
                    x.DataNode == dn && x.LinkedDataNode == linkedDn ||
                    x.DataNode == linkedDn && x.LinkedDataNode == dn);
                var isLinkInCurrPeriod = currentYearData.Any(x =>
                    x.DataNode == dn && x.LinkedDataNode == linkedDn ||
                    x.DataNode == linkedDn && x.LinkedDataNode == dn);

                if (isLinkInPrevPeriod && !isLinkInCurrPeriod)
                {
                    if (param.ReinsuranceCoverage != ((double) previousYear) / 1000)
                        errors.Add(
                            $"Current Reinsurance Coverage for {dn} linked to {linkedDn} not matching:\n Expected {((double) previousYear) / 1000} \n Computed {param.ReinsuranceCoverage}.");
                }
                else if (param.ReinsuranceCoverage != ((double) currentYear) / 1000)
                    errors.Add(
                        $"Current Reinsurance Coverage for {dn} linked to {linkedDn} not matching:\n Expected {((double) currentYear) / 1000} \n Computed {param.ReinsuranceCoverage}.");
            }

            //Previous Period
            computedCount = interDataNodeParameters[dn][Consts.PreviousPeriod].Count();

            if (currentYearData.Any())
            {
                if (computedCount != currentLinksCount)
                    errors.Add(
                        $"Previous DataNode parameter count for {dn} not matching:\n Expected {currentLinksCount} \n Computed {computedCount}.");
            }
            else if (computedCount != previousLinksCount)
                errors.Add(
                    $"Previous DataNode parameter count for {dn} not matching:\n Expected {previousLinksCount} \n Computed {computedCount}.");

            foreach (var param in interDataNodeParameters[dn][Consts.PreviousPeriod])
            {
                var linkedDn = param.DataNode == dn ? param.LinkedDataNode : param.DataNode;
                var isLinkInPrevPeriod = previousYearData.Any(x =>
                    x.DataNode == dn && x.LinkedDataNode == linkedDn ||
                    x.DataNode == linkedDn && x.LinkedDataNode == dn);
                var isLinkInCurrPeriod = currentYearData.Any(x =>
                    x.DataNode == dn && x.LinkedDataNode == linkedDn ||
                    x.DataNode == linkedDn && x.LinkedDataNode == dn);

                if (!isLinkInPrevPeriod && isLinkInCurrPeriod)
                {
                    if (param.ReinsuranceCoverage != ((double) currentYear) / 1000)
                        errors.Add(
                            $"Previous Reinsurance Coverage for {dn} linked to {linkedDn} not matching:\n Expected {((double) currentYear) / 1000} \n Computed {param.ReinsuranceCoverage}.");
                }
                else if (param.ReinsuranceCoverage != ((double) previousYear) / 1000)
                    errors.Add(
                        $"Previous Reinsurance Coverage for {dn} linked to {linkedDn} not matching:\n Expected {((double) previousYear) / 1000} \n Computed {param.ReinsuranceCoverage}.");
            }
        }

        Work.Reset(x => x.ResetCurrentPartitions());

        if (errors.Any()) ApplicationMessage.Log(Error.Generic, string.Join("\n", errors));
        return Activity.Finish();
    }

    [Fact]
    public async Task Test29Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);
        int previousYear = 2019;

        var testData = new InterDataNodeParameter[]
        {
            new InterDataNodeParameter
            {
                DataNode = gic, LinkedDataNode = gric1, ReinsuranceCoverage = ((double) previousYear) / 1000,
                Year = previousYear, Month = 3
            },
            new InterDataNodeParameter
            {
                DataNode = gic2, LinkedDataNode = gric1, ReinsuranceCoverage = ((double) previousYear) / 1000,
                Year = previousYear, Month = 3
            },
            new InterDataNodeParameter
            {
                DataNode = gric1, LinkedDataNode = xgic, ReinsuranceCoverage = ((double) previousYear) / 1000,
                Year = previousYear, Month = 3
            },
        };

        var activity = await CheckLoadInterDataNodeParameterAsync(args, testData, previousYear);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test30Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);
        int previousYear = 2019;
        int currentYear = 2020;

        var testData = new InterDataNodeParameter[]
        {
            new InterDataNodeParameter
            {
                DataNode = gic, LinkedDataNode = gric1, ReinsuranceCoverage = ((double) previousYear) / 1000,
                Year = previousYear, Month = 3
            },
            new InterDataNodeParameter
            {
                DataNode = gric1, LinkedDataNode = xgic, ReinsuranceCoverage = ((double) previousYear) / 1000,
                Year = previousYear, Month = 3
            },
            new InterDataNodeParameter
            {
                DataNode = gic, LinkedDataNode = gric1, ReinsuranceCoverage = ((double) currentYear) / 1000,
                Year = currentYear, Month = 9
            },
            new InterDataNodeParameter
            {
                DataNode = gic2, LinkedDataNode = gric1, ReinsuranceCoverage = ((double) currentYear) / 1000,
                Year = currentYear, Month = 9
            },
            new InterDataNodeParameter
            {
                DataNode = gric1, LinkedDataNode = xgic, ReinsuranceCoverage = ((double) currentYear) / 1000,
                Year = currentYear, Month = 9
            },
        };

        var activity = await CheckLoadInterDataNodeParameterAsync(args, testData, previousYear, currentYear);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test31Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);
        int previousYear = 2019;
        int currentYear = 2020;

        var testData = new InterDataNodeParameter[]
        {
            new InterDataNodeParameter
            {
                DataNode = gic, LinkedDataNode = gric1, ReinsuranceCoverage = ((double) previousYear) / 1000,
                Year = previousYear, Month = 3
            },
            new InterDataNodeParameter
            {
                DataNode = gric1, LinkedDataNode = xgic, ReinsuranceCoverage = ((double) previousYear) / 1000,
                Year = previousYear, Month = 3
            },
        };

        var activity = await CheckLoadInterDataNodeParameterAsync(args, testData, previousYear, currentYear);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test32Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);
        int previousYear = 2019;
        int currentYear = 2020;

        var testData = new InterDataNodeParameter[]
        {
            new InterDataNodeParameter
            {
                DataNode = gic, LinkedDataNode = gric1, ReinsuranceCoverage = ((double) currentYear) / 1000,
                Year = currentYear, Month = 3
            },
            new InterDataNodeParameter
            {
                DataNode = gric1, LinkedDataNode = xgic, ReinsuranceCoverage = ((double) currentYear) / 1000,
                Year = currentYear, Month = 3
            },
        };

        var activity = await CheckLoadInterDataNodeParameterAsync(args, testData, previousYear, currentYear);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }


    public async Task<ActivityLog> CheckLoadCurrentParameterAsync<T>(Args args, IEnumerable<T> testData,
        (int Year, int Month, string Scenario) expectedCurrentPeriod,
        Func<T, string> identityExpression)
        where T : IWithYearMonthAndScenario
    {
        Activity.Start();
        await Work.UpdateAsync(testData);
        var firstParameter = (await Work.LoadCurrentParameterAsync<T>(args, identityExpression)).First();

        //Check Current Period
        firstParameter.Value.Year.Should().Be(expectedCurrentPeriod.Year);
        firstParameter.Value.Month.Should().Be(expectedCurrentPeriod.Month);
        firstParameter.Value.Scenario.Should().Be(expectedCurrentPeriod.Scenario);

        await Work.DeleteAsync(Work.Query<T>().ToArray());
        return Activity.Finish();
    }

    [Fact]
    public async Task Test33Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new PartnerRating[]
        {
            new PartnerRating {Partner = "PT1", Year = 2020, Month = 9, Scenario = scenario},
            new PartnerRating {Partner = "PT1", Year = 2019, Month = 12, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentParameterAsync<PartnerRating>(args, testData, (2020, 9, scenario), x => x.Partner);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test34Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, null);

        var testData = new PartnerRating[]
        {
            new PartnerRating {Partner = "PT1", Year = 2020, Month = 6, Scenario = null},
            new PartnerRating {Partner = "PT1", Year = 2019, Month = 3, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentParameterAsync<PartnerRating>(args, testData, (2020, 6, null), x => x.Partner);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test35Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new PartnerRating[]
        {
            new PartnerRating {Partner = "PT1", Year = 2020, Month = 9, Scenario = scenario},
            new PartnerRating {Partner = "PT1", Year = 2019, Month = 12, Scenario = scenario},
            new PartnerRating {Partner = "PT1", Year = 2019, Month = 12, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentParameterAsync<PartnerRating>(args, testData, (2020, 9, scenario), x => x.Partner);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test36Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new PartnerRating[]
        {
            new PartnerRating {Partner = "PT1", Year = 2020, Month = 9, Scenario = null},
            new PartnerRating {Partner = "PT1", Year = 2020, Month = 9, Scenario = scenario},
            new PartnerRating {Partner = "PT1", Year = 2019, Month = 12, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentParameterAsync<PartnerRating>(args, testData, (2020, 9, scenario), x => x.Partner);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test37Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new CreditDefaultRate[]
        {
            new CreditDefaultRate {CreditRiskRating = "A", Year = 2020, Month = 9, Scenario = scenario},
            new CreditDefaultRate {CreditRiskRating = "A", Year = 2019, Month = 12, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentParameterAsync<CreditDefaultRate>(args, testData, (2020, 9, scenario),
                x => x.CreditRiskRating);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test38Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new CreditDefaultRate[]
        {
            new CreditDefaultRate {CreditRiskRating = "A", Year = 2020, Month = 9, Scenario = null},
            new CreditDefaultRate {CreditRiskRating = "A", Year = 2019, Month = 12, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentParameterAsync<CreditDefaultRate>(args, testData, (2020, 9, null),
                x => x.CreditRiskRating);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test39Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new CreditDefaultRate[]
        {
            new CreditDefaultRate {CreditRiskRating = "A", Year = 2020, Month = 9, Scenario = scenario},
            new CreditDefaultRate {CreditRiskRating = "A", Year = 2019, Month = 12, Scenario = scenario},
            new CreditDefaultRate {CreditRiskRating = "A", Year = 2019, Month = 12, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentParameterAsync<CreditDefaultRate>(args, testData, (2020, 9, scenario),
                x => x.CreditRiskRating);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    [Fact]
    public async Task Test40Async()
    {
        var args = new Args("CH", 2020, 9, Periodicity.Monthly, scenario);

        var testData = new CreditDefaultRate[]
        {
            new CreditDefaultRate {CreditRiskRating = "PT1", Year = 2020, Month = 9, Scenario = null},
            new CreditDefaultRate {CreditRiskRating = "PT1", Year = 2020, Month = 9, Scenario = scenario},
            new CreditDefaultRate {CreditRiskRating = "PT1", Year = 2019, Month = 12, Scenario = null}
        };

        var activity =
            await CheckLoadCurrentParameterAsync<CreditDefaultRate>(args, testData, (2020, 9, scenario),
                x => x.CreditRiskRating);


        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }
}