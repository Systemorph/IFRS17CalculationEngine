//#!import "../Import/Importers"


//#!import "TestData"


using FluentAssertions;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Validations;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Tests;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Import;
using Systemorph.Vertex.Scopes.Proxy;
using Systemorph.Vertex.Workspace;

namespace OpenSmc.Ifrs17.Domain.Test;

public class DataNodeParameterTest : TestBase
{

    public DataNodeParameterTest(IImportVariable import, IDataSource dataSource,
        IWorkspaceVariable work, IActivityVariable activity, IScopeFactory scopes) : 
        base(import, dataSource, work, activity, scopes){}

    private async Task InitAsync()
    {
        TestData.InitializeAsync();
        await Import.FromString(TestData.novelties).WithType<Novelty>()
            .WithTarget(DataSource)
            .ExecuteAsync();
        await Import.FromString(TestData.canonicalAocTypes)
            .WithType<AocType>()
            .WithTarget(DataSource)
            .ExecuteAsync();

        await Import.FromString(TestData.canonicalAocConfig)
            .WithFormat(ImportFormats.AocConfiguration).WithTarget(DataSource)
            .ExecuteAsync();


        await DataSource.UpdateAsync(TestData.reportingNodes);
        await DataSource.UpdateAsync<Portfolio>(TestData.dt1.RepeatOnce());
        await DataSource.UpdateAsync<Portfolio>(TestData.dtr1.RepeatOnce());
        await DataSource.UpdateAsync<GroupOfInsuranceContract>(TestData.dt11.RepeatOnce());

        await DataSource.UpdateAsync<GroupOfReinsuranceContract>(TestData.dtr11.RepeatOnce());


        await DataSource.UpdateAsync(new[]
        {
            TestData.dt11State, TestData.dtr11State
        });


        await Import.FromString(TestData.estimateType)
            .WithType<EstimateType>().WithTarget(DataSource).ExecuteAsync();
        await Import.FromString(TestData.economicBasis)
            .WithType<EconomicBasis>().WithTarget(DataSource).ExecuteAsync();


        await DataSource.UpdateAsync(TestData.yieldCurvePrevious.RepeatOnce());


        await DataSource.UpdateAsync(new[]
        {
            TestData.partition, TestData.previousPeriodPartition
        });

        await DataSource.UpdateAsync(TestData.partitionReportingNode.RepeatOnce());


        Work.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());
    }


    public async Task<ActivityLog> TestValidation(string inputFile, List<string> errorBms)
    {
        var ws = Work.CreateNew();
        ws.InitializeFrom(DataSource);
        Activity.Start();
        var log = await Import.FromString(inputFile).WithFormat(ImportFormats.DataNodeParameter).WithTarget(ws)
            .ExecuteAsync();
        log.Errors.Count().Should().Be(errorBms.Count());
        errorBms.Intersect(log.Errors.Select(x => x.ToString().Substring(0, x.ToString().Length - 2).Substring(40))
            .ToArray()).Count().Should().Be(errorBms.Count());
        return Activity.Finish();
    }

    public async Task<bool> CheckDefaultEbDriver((string va, string lt) key, string eb, string inputFile)
    {
        var ws = Work.CreateNew();
        ws.InitializeFrom(DataSource);
        ws.InitializeFrom(DataSource);
        await ws.DeleteAsync(ws.Query<GroupOfInsuranceContract>());
        await ws.UpdateAsync<GroupOfInsuranceContract>(new[]
            {TestData.dt11 with {ValuationApproach = key.va, LiabilityType = key.lt}});

        var log = await Import.FromString(inputFile).WithFormat(ImportFormats.DataNodeParameter).WithTarget(ws)
            .ExecuteAsync();
        log.Status.Should().Be(ActivityLogStatus.Succeeded);
        return (await ws.Query<SingleDataNodeParameter>().ToArrayAsync()).Single().EconomicBasisDriver == eb;
    }

    public async Task Test1Async()
    {
        string inputFile =
            @"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.9,Monthly,Uniform
DataNodeInvalid0,0.85,Monthly,Uniform

@@InterDataNodeParameter
DataNode,LinkedDataNode,ReinsuranceCoverage
DTR1.1,DT1.1,1
DataNodeInvalid1,DTR1.1,1
DTR1.1,DataNodeInvalid2,1";

        var errorsBm = new List<string>()
        {
            Error.InvalidDataNode.GetMessage("DataNodeInvalid0"),
            Error.InvalidDataNode.GetMessage("DataNodeInvalid1"),
            Error.InvalidDataNode.GetMessage("DataNodeInvalid2")
        };


        var activity = await TestValidation(inputFile, errorsBm);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test2Async()
    {
        string inputFile =
            @"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation
DT1.1,0.9
DT1.1,0.9

@@InterDataNodeParameter
DataNode,LinkedDataNode,ReinsuranceCoverage
DTR1.1,DT1.1,1
DT1.1,DTR1.1,1
";

        var errorsBm = new List<string>()
        {
            Error.DuplicateSingleDataNode.GetMessage("DT1.1"),
            Error.DuplicateInterDataNode.GetMessage("DT1.1", "DTR1.1"),
        };


        var activity = await TestValidation(inputFile, errorsBm);
        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }


    public async Task Test3Async()
    {
        var minimalParametersScenario = @"
@@Main
ReportingNode,Year,Month
CH,2020,12
@@SingleDataNodeParameter
DataNode
DT1.1
@@InterDataNodeParameter
DataNode,LinkedDataNode,ReinsuranceCoverage
DT1.1,DTR1.1,0.62";


        var log = await Import.FromString(minimalParametersScenario).WithFormat(ImportFormats.DataNodeParameter)
            .WithTarget(DataSource).ExecuteAsync();

        log.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test4Async()
    {
        var inputFile =
            @"
@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,
DT1.1,0.85,
@@InterDataNodeParameter
DataNode,LinkedDataNode,ReinsuranceCoverage
DT1.1,DT1.1,1
";

        var errorsBm = new List<string>() {Error.ReinsuranceCoverageDataNode.GetMessage("DT1.1", "DT1.1")};


        var activity = await TestValidation(inputFile, errorsBm);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test5Async()
    {
        var inputFile =
            @"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.85,Monthly,InvalidEntry
";
        var errorsBm = new List<string>() {Error.InvalidInterpolationMethod.GetMessage("DT1.1"),};


        var activity = await TestValidation(inputFile, errorsBm);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test6Async()
    {
        var inputFile =
            @"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.85,Monthly,,
";
        var errorsBm = new List<string>() { }; //Get(Error.InvalidInterpolationMethod, "DT1.1"),};


        var activity = await TestValidation(inputFile, errorsBm);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test7Async()
    {
        var inputFile =
            @"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.85,Yearly,,
";
        var errorsBm = new List<string>() {Error.InvalidInterpolationMethod.GetMessage("DT1.1"),};


        var activity = await TestValidation(inputFile, errorsBm);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test8Async()
    {
        var inputFile =
            @"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.85,A,,
";
        var errorsBm = new List<string>() {Error.InvalidCashFlowPeriodicity.GetMessage("DT1.1"),};


        var activity = await TestValidation(inputFile, errorsBm);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test9Async()
    {
        var inputFile =
            @"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.85,,Uniform,
";
        var errorsBm = new List<string>() {Error.InvalidCashFlowPeriodicity.GetMessage("DT1.1"),};


        var activity = await TestValidation(inputFile, errorsBm);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test10Async()
    {
        var inputFile =
            @"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.85,,,
";
        var errorsBm = new List<string>() {Error.InvalidCashFlowPeriodicity.GetMessage("DT1.1"),};


        var activity = await TestValidation(inputFile, errorsBm);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test11Async()
    {
        var inputFile =
            @"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,EconomicBasisDriver
DT1.1,0.85,A
";
        var errorsBm = new List<string>() {Error.InvalidEconomicBasisDriver.GetMessage("DT1.1"),};


        var activity = await TestValidation(inputFile, errorsBm);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test12Async()
    {
        var inputFile =
            @"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,EconomicBasisDriver
DT1.1,0.85,
";


        var economicBasisDriverByValuationApproach = new Dictionary<(string, string), string>
        {
            {(ValuationApproaches.BBA, LiabilityTypes.LIC), EconomicBases.L},
            {(ValuationApproaches.BBA, LiabilityTypes.LRC), EconomicBases.L},
            {(ValuationApproaches.VFA, LiabilityTypes.LIC), EconomicBases.C},
            {(ValuationApproaches.VFA, LiabilityTypes.LRC), EconomicBases.C},
            {(ValuationApproaches.PAA, LiabilityTypes.LIC), EconomicBases.C},
            {(ValuationApproaches.PAA, LiabilityTypes.LRC), EconomicBases.N},
        };


        var comparison = new Dictionary<(string, string), bool>();
        foreach (var kvp in economicBasisDriverByValuationApproach)
            comparison[kvp.Key] = await CheckDefaultEbDriver(kvp.Key, kvp.Value, inputFile);


        comparison.All(x => x.Value).Should().BeTrue();
    }

    public async Task Test13Async()
    {
        var inputFile =
            @"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,ReleasePattern0,ReleasePattern1
DT1.1,0.85,InvalidValue0,InvalidValue1
";

        var errorsBm = new List<string>()
        {
            Error.ParsingInvalidOrScientificValue.GetMessage("InvalidValue0"),
            Error.ParsingInvalidOrScientificValue.GetMessage("InvalidValue1")
        };


        var activity = await TestValidation(inputFile, errorsBm);

        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }

    public async Task Test14Async()
    {
        var inputFile =
            @"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,ReleasePattern0,ReleasePattern1
DT1.1,0.1,1,2
DTR1.1,0.85
";
        var errorsBm = new List<string>() { };


        var activity = await TestValidation(inputFile, errorsBm);
        activity.Status.Should().Be(ActivityLogStatus.Succeeded);
    }
}


