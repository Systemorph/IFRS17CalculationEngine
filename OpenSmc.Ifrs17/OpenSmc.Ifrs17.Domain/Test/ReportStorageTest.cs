using System.Diagnostics;
using FluentAssertions;
using Microsoft.Graph;
using Microsoft.Graph.SecurityNamespace;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Report;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Workspace;

//#!import "../Report/ReportStorage"
//#!import "TestData"


await DataSource.UpdateAsync(reportingNodes);
await DataSource.UpdateAsync(new[]{partition, previousPeriodPartition});
await Import.FromString(projectionConfiguration).WithType<ProjectionConfiguration>().WithTarget(DataSource).ExecuteAsync();


Workspace.Initialize(x => x.FromSource(DataSource).DisableInitialization<ExchangeRate>());


public async Task<ActivityLog> CheckGetFx(string currentCurrency, string targetCurrency, int year, int month, IEnumerable<ExchangeRate> testData, double fxBOPBenchmark, double fxAVGBenchmark, double fxEOPBenchmark)
{
    Activity.Start();
    await Workspace.UpdateAsync(testData);
    
    //Create report storage
    var period = (year, month);
    var reportStorage = new ReportStorage(Workspace, Report, Export);
    await reportStorage.InitializeReportIndependentCacheAsync();
    await reportStorage.InitializeAsync(period, "G", null, CurrencyType.Contractual);
    
    var fxBOP = reportStorage.GetFx(period, currentCurrency, targetCurrency, FxPeriod.BeginningOfPeriod);
    var fxAVG = reportStorage.GetFx(period, currentCurrency, targetCurrency, FxPeriod.Average);
    var fxEOP = reportStorage.GetFx(period, currentCurrency, targetCurrency, FxPeriod.EndOfPeriod);
    
    //Check FX rates
    fxBOP.Should().Be(fxBOPBenchmark);
    fxAVG.Should().Be(fxAVGBenchmark);
    fxEOP.Should().Be(fxEOPBenchmark);
    
    await Workspace.DeleteAsync(Workspace.Query<ExchangeRate>().ToArray());
    return Activity.Finish();
}


var testData = new ExchangeRate[] {new ExchangeRate{ Currency = "EUR", Year = 2020, Month = 12, FxType = FxType.Spot, FxToGroupCurrency = 10 }, 
                                 new ExchangeRate{ Currency = "EUR", Year = 2021, Month = 3, FxType = FxType.Spot, FxToGroupCurrency = 15 },
                                 new ExchangeRate{ Currency = "EUR", Year = 2021, Month = 6, FxType = FxType.Average, FxToGroupCurrency = 20 },
                                 new ExchangeRate{ Currency = "EUR", Year = 2021, Month = 6, FxType = FxType.Spot, FxToGroupCurrency = 30 },
                                 new ExchangeRate{ Currency = "USD", Year = 2020, Month = 12, FxType = FxType.Spot, FxToGroupCurrency = 5 },
                                 new ExchangeRate{ Currency = "USD", Year = 2021, Month = 6, FxType = FxType.Average, FxToGroupCurrency = 2 },
                                 new ExchangeRate{ Currency = "USD", Year = 2021, Month = 6, FxType = FxType.Spot, FxToGroupCurrency = 0.5 }};



var activity = await CheckGetFx("EUR", "USD", 2021, 6, testData, 2, 10, 60);
activity


activity.Status.Should().Be(ActivityLogStatus.Succeeded);


Workspace.Reset(x => x.ResetInitializationRules().ResetCurrentPartitions());



