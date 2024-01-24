//#!import "../Import/Importers"


//#!import "TestData"


using System.Diagnostics;
using FluentAssertions;
using Microsoft.Graph.SecurityNamespace;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Import;
using Systemorph.Vertex.Workspace;

public class DataNodeParameterTest
{
    protected IImportVariable Import;
    protected IWorkspaceVariable Workspace;
    protected IDataSource DataSource;
    protected IActivityVariable Activity;
    private TestData testData { get; set; }

    public DataNodeParameterTest(IImportVariable import, IWorkspaceVariable workspace,
        IDataSource dataSource, IActivityVariable activity)
    {
        Import = import;
        Workspace = workspace;
        DataSource = dataSource;
        Activity = activity;
        testData = new TestData();
    }

    private async Task InitAsync()
    {
        testData.InitializeAsync();
        await Import.FromString(testData.novelties).WithType<Novelty>()
            .WithTarget(DataSource)
            .ExecuteAsync();
        await Import.FromString(testData.canonicalAocTypes)
            .WithType<AocType>()
            .WithTarget(DataSource)
            .ExecuteAsync();

        await Import.FromString(testData.canonicalAocConfig)
            .WithFormat(ImportFormats.AocConfiguration).WithTarget(DataSource)
            .ExecuteAsync();


        await DataSource.UpdateAsync(testData.reportingNodes);
        await DataSource.UpdateAsync<Portfolio>(testData.dt1.RepeatOnce());
        await DataSource.UpdateAsync<Portfolio>(testData.dtr1.RepeatOnce());
        await DataSource.UpdateAsync<GroupOfInsuranceContract>(testData.dt11.RepeatOnce());

        await DataSource.UpdateAsync<GroupOfReinsuranceContract>(testData.dtr11.RepeatOnce());


        await DataSource.UpdateAsync(new[]
        {
            testData.dt11State, testData.dtr11State
        });


        await Import.FromString(testData.estimateType)
            .WithType<EstimateType>().WithTarget(DataSource).ExecuteAsync();
        await Import.FromString(testData.economicBasis)
            .WithType<EconomicBasis>().WithTarget(DataSource).ExecuteAsync();


        await DataSource.UpdateAsync(testData.yieldCurvePrevious.RepeatOnce());


        await DataSource.UpdateAsync(new[]
        {
            testData.partition, testData.previousPeriodPartition
        });

        await DataSource.UpdateAsync(testData.partitionReportingNode.RepeatOnce());


        Workspace.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>(
        ).DisableInitialization<IfrsVariable>());
    }


    public async Task<ActivityLog> TestValidation(string inputFile, List<string> errorBms)
    {
        var ws = Workspace.CreateNew();
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
        var ws = Workspace.CreateNew();
        ws.InitializeFrom(DataSource);
        ws.InitializeFrom(DataSource);
        await ws.DeleteAsync(ws.Query<GroupOfInsuranceContract>());
        await ws.UpdateAsync<GroupOfInsuranceContract>(new[]
            {testData.dt11 with {ValuationApproach = key.va, LiabilityType = key.lt}});

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


