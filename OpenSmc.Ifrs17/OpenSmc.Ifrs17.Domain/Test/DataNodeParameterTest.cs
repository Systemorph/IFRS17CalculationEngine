#!import "../Import/Importers"


#!import "TestData"


await Import.FromString(novelties).WithType<Novelty>().WithTarget(DataSource).ExecuteAsync();
await Import.FromString(canonicalAocTypes).WithType<AocType>().WithTarget(DataSource).ExecuteAsync();
await Import.FromString(canonicalAocConfig).WithFormat(ImportFormats.AocConfiguration).WithTarget(DataSource).ExecuteAsync();


await DataSource.UpdateAsync(reportingNodes);
await DataSource.UpdateAsync<Portfolio>(dt1.RepeatOnce());
await DataSource.UpdateAsync<Portfolio>(dtr1.RepeatOnce());
await DataSource.UpdateAsync<GroupOfInsuranceContract>(new [] {dt11});
await DataSource.UpdateAsync<GroupOfReinsuranceContract>(new [] {dtr11});


await DataSource.UpdateAsync(new [ ] {dt11State,dtr11State});


await Import.FromString(estimateType).WithType<EstimateType>().WithTarget(DataSource).ExecuteAsync();
await Import.FromString(economicBasis).WithType<EconomicBasis>().WithTarget(DataSource).ExecuteAsync();


await DataSource.UpdateAsync(new [ ] {yieldCurvePrevious});


await DataSource.UpdateAsync(new[]{partition, previousPeriodPartition});
await DataSource.UpdateAsync(new[]{partitionReportingNode});


Workspace.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());


public async Task<ActivityLog> TestValidation(string inputFile,  List<string> errorBms){
    var ws = Workspace.CreateNew();
    ws.InitializeFrom(DataSource);
    Activity.Start();
    var log = await Import.FromString(inputFile).WithFormat(ImportFormats.DataNodeParameter).WithTarget(ws).ExecuteAsync();
    log.Errors.Count().Should().Be(errorBms.Count());
    errorBms.Intersect(log.Errors.Select(x => x.ToString().Substring(0,x.ToString().Length-2).Substring(40)).ToArray()).Count().Should().Be(errorBms.Count());
    return Activity.Finish();
}


var inputFile = 
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

var errorsBm = new List<string>(){Error.InvalidDataNode.GetMessage("DataNodeInvalid0"),
                                  Error.InvalidDataNode.GetMessage("DataNodeInvalid1"),
                                  Error.InvalidDataNode.GetMessage("DataNodeInvalid2")};


var activity = await TestValidation(inputFile, errorsBm);
activity


var inputFile = 
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
var errorsBm = new List<string>(){Error.DuplicateSingleDataNode.GetMessage("DT1.1"),
                                  Error.DuplicateInterDataNode.GetMessage("DT1.1","DTR1.1"),};


var activity = await TestValidation(inputFile, errorsBm);
activity


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


var log = await Import.FromString(minimalParametersScenario).WithFormat(ImportFormats.DataNodeParameter).WithTarget(DataSource).ExecuteAsync();
log.Status.Should().Be(ActivityLogStatus.Succeeded);


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
var errorsBm = new List<string>(){Error.ReinsuranceCoverageDataNode.GetMessage("DT1.1","DT1.1")};


var activity = await TestValidation(inputFile, errorsBm);
activity


var inputFile = 
@"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.85,Monthly,InvalidEntry
";
var errorsBm = new List<string>(){Error.InvalidInterpolationMethod.GetMessage("DT1.1"),};


var activity = await TestValidation(inputFile, errorsBm);
activity


var inputFile = 
@"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.85,Monthly,,
";
var errorsBm = new List<string>(){};//Get(Error.InvalidInterpolationMethod, "DT1.1"),};


var activity = await TestValidation(inputFile, errorsBm);
activity


var inputFile = 
@"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.85,Yearly,,
";
var errorsBm = new List<string>(){Error.InvalidInterpolationMethod.GetMessage("DT1.1"),};


var activity = await TestValidation(inputFile, errorsBm);
activity


var inputFile = 
@"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.85,A,,
";
var errorsBm = new List<string>(){Error.InvalidCashFlowPeriodicity.GetMessage("DT1.1"),};


var activity = await TestValidation(inputFile, errorsBm);
activity


var inputFile = 
@"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.85,,Uniform,
";
var errorsBm = new List<string>(){Error.InvalidCashFlowPeriodicity.GetMessage("DT1.1"),};


var activity = await TestValidation(inputFile, errorsBm);
activity


var inputFile = 
@"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,CashFlowPeriodicity,InterpolationMethod
DT1.1,0.85,,,
";
var errorsBm = new List<string>(){Error.InvalidCashFlowPeriodicity.GetMessage("DT1.1"),};


var activity = await TestValidation(inputFile, errorsBm);
activity


var inputFile = 
@"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,EconomicBasisDriver
DT1.1,0.85,A
";
var errorsBm = new List<string>(){Error.InvalidEconomicBasisDriver.GetMessage("DT1.1"),};


var activity = await TestValidation(inputFile, errorsBm);
activity


var inputFile = 
@"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,EconomicBasisDriver
DT1.1,0.85,
";


var economicBasisDriverByValuationApproach = new Dictionary<(string,string),string>{
     {(ValuationApproaches.BBA, LiabilityTypes.LIC), EconomicBases.L},
     {(ValuationApproaches.BBA, LiabilityTypes.LRC), EconomicBases.L},
     {(ValuationApproaches.VFA, LiabilityTypes.LIC), EconomicBases.C},
     {(ValuationApproaches.VFA, LiabilityTypes.LRC), EconomicBases.C},
     {(ValuationApproaches.PAA, LiabilityTypes.LIC), EconomicBases.C},
     {(ValuationApproaches.PAA, LiabilityTypes.LRC), EconomicBases.N},
    };


public async Task<bool> CheckDefaultEbDriver((string va, string lt) key, string eb){
    var ws = Workspace.CreateNew();
    ws.InitializeFrom(DataSource);
    ws.InitializeFrom(DataSource);
    await ws.DeleteAsync(ws.Query<GroupOfInsuranceContract>());
    await ws.UpdateAsync<GroupOfInsuranceContract>(new [] {dt11 with {ValuationApproach = key.va, LiabilityType = key.lt}});
    
    var log = await Import.FromString(inputFile).WithFormat(ImportFormats.DataNodeParameter).WithTarget(ws).ExecuteAsync();
    log.Status.Should().Be(ActivityLogStatus.Succeeded);
    return (await ws.Query<SingleDataNodeParameter>().ToArrayAsync()).Single().EconomicBasisDriver == eb;
}


var comparison = new Dictionary<(string,string),bool>();
foreach (var kvp in economicBasisDriverByValuationApproach)
    comparison[kvp.Key] = await CheckDefaultEbDriver(kvp.Key, kvp.Value);


comparison.All(x => x.Value).Should().BeTrue();


var inputFile = 
@"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,ReleasePattern0,ReleasePattern1
DT1.1,0.85,InvalidValue0,InvalidValue1
";
var errorsBm = new List<string>(){Error.ParsingInvalidOrScientificValue.GetMessage("InvalidValue0"), Error.ParsingInvalidOrScientificValue.GetMessage("InvalidValue1")};


var activity = await TestValidation(inputFile, errorsBm);
activity


var inputFile = 
@"@@Main
ReportingNode,Year,Month
CH,2020,12

@@SingleDataNodeParameter
DataNode,PremiumAllocation,ReleasePattern0,ReleasePattern1
DT1.1,0.1,1,2
DTR1.1,0.85
";
var errorsBm = new List<string>(){};


var activity = await TestValidation(inputFile, errorsBm);
activity



