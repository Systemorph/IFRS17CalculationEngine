using FluentAssertions;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Report;
using OpenSmc.Ifrs17.Domain.Tests;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Export.Factory;
using Systemorph.Vertex.Import;
using Systemorph.Vertex.Pivot.Builder.Interfaces;
using Systemorph.Vertex.Scopes.Proxy;
using Systemorph.Vertex.Workspace;

//#!import "../Report/ReportStorage"
//#!import "TestData"

public class ReportStorageTest : TestBase
{
    private readonly IPivotFactory report;
    private readonly IExportVariable export;

    public ReportStorageTest(IImportVariable import, IDataSource dataSource,
        IWorkspaceVariable work, IActivityVariable activity, IScopeFactory scopes,
        IPivotFactory report, IExportVariable export) :
        base(import, dataSource, work, activity, scopes)
    {
        this.report = report;
        this.export = export;
    }

    public async Task InitializeAsync()
    {
        await TestData.InitializeAsync();
        await DataSource.UpdateAsync(TestData.reportingNodes);
        await DataSource.UpdateAsync(new[]
        {
            TestData.partition, TestData.previousPeriodPartition
        });

        await Import.FromString(TestData.projectionConfiguration).WithType<ProjectionConfiguration>(
        ).WithTarget(DataSource).ExecuteAsync();


        Work.Initialize(x => x.FromSource(DataSource).DisableInitialization<ExchangeRate>());
    }


    public async Task<ActivityLog> CheckGetFx(string currentCurrency, string targetCurrency, int year, int month,
        IEnumerable<ExchangeRate> testData, double fxBOPBenchmark, double fxAVGBenchmark, double fxEOPBenchmark)
    {
        Activity.Start();
        await Work.UpdateAsync(testData);

        //Create report storage
        var period = (year, month);
        var reportStorage = new ReportStorage(Work, report, export);
        await reportStorage.InitializeReportIndependentCacheAsync();
        await reportStorage.InitializeAsync(period, "G", null, CurrencyType.Contractual);

        var fxBOP = reportStorage.GetFx(period, currentCurrency, targetCurrency, FxPeriod.BeginningOfPeriod);
        var fxAVG = reportStorage.GetFx(period, currentCurrency, targetCurrency, FxPeriod.Average);
        var fxEOP = reportStorage.GetFx(period, currentCurrency, targetCurrency, FxPeriod.EndOfPeriod);

        //Check FX rates
        fxBOP.Should().Be(fxBOPBenchmark);
        fxAVG.Should().Be(fxAVGBenchmark);
        fxEOP.Should().Be(fxEOPBenchmark);

        await Work.DeleteAsync(Work.Query<ExchangeRate>().ToArray());
        return Activity.Finish();
    }

    public async Task Test()
    {
        var testData = new ExchangeRate[]
        {
            new ExchangeRate {Currency = "EUR", Year = 2020, Month = 12, FxType = FxType.Spot, FxToGroupCurrency = 10},
            new ExchangeRate {Currency = "EUR", Year = 2021, Month = 3, FxType = FxType.Spot, FxToGroupCurrency = 15},
            new ExchangeRate
                {Currency = "EUR", Year = 2021, Month = 6, FxType = FxType.Average, FxToGroupCurrency = 20},
            new ExchangeRate {Currency = "EUR", Year = 2021, Month = 6, FxType = FxType.Spot, FxToGroupCurrency = 30},
            new ExchangeRate {Currency = "USD", Year = 2020, Month = 12, FxType = FxType.Spot, FxToGroupCurrency = 5},
            new ExchangeRate {Currency = "USD", Year = 2021, Month = 6, FxType = FxType.Average, FxToGroupCurrency = 2},
            new ExchangeRate {Currency = "USD", Year = 2021, Month = 6, FxType = FxType.Spot, FxToGroupCurrency = 0.5}
        };



        var activity = await CheckGetFx("EUR", "USD", 2021, 6, testData, 2, 10, 60);



        activity.Status.Should().Be(ActivityLogStatus.Succeeded);


        Work.Reset(x => x.ResetInitializationRules().ResetCurrentPartitions());
    }
}



