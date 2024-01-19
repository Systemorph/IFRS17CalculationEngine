string novelties = 
@"@@Novelty
SystemName,DisplayName,Parent,Order
I,In Force,,1
N,New Business,,10
C,Combined,,20";

string canonicalAocTypes = 
@"@@AocType,,,,,,,,,,,
SystemName,DisplayName,Parent,Order,,,,,,,,
BOP,Opening Balance,,10,,,,,,,,
MC,Model Correction,,20,,,,,,,,
PC,Portfolio Changes,,30,,,,,,,,
RCU,Reinsurance Coverage Update,PC,40,,,,,,,,
CF,Cash flow,,50,,,,,,,,
IA,Interest Accretion,,60,,,,,,,,
AU,Assumption Update,,70,,,,,,,,
FAU,Financial Assumption Update,,80,,,,,,,,
YCU,Yield Curve Update,FAU,90,,,,,,,,
CRU,Credit Risk Update,FAU,100,,,,,,,,
EV,Experience Variance,,110,,,,,,,,
WO,Write-Off,,120,,,,,,,,
CL,Combined Liabilities,,130,,,,,,,,
EA,Experience Adjustment,,140,,,,,,,,
AM,Amortization,,150,,,,,,,,
FX,FX Impact,,160,,,,,,,,
EOP,Closing Balance,,170,,,,,,,,";

string canonicalAocConfig = 
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
CL,C,2,4,10,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,180,1900,1
EA,C,4,4,8,EndOfPeriod,NotApplicable,NotApplicable,NotApplicable,EndOfPeriod,190,1900,1
CF,C,17,6,4,Average,NotApplicable,NotApplicable,NotApplicable,NotApplicable,193,1900,1
WO,C,17,2,4,Average,NotApplicable,NotApplicable,NotApplicable,NotApplicable,195,1900,1
AM,C,4,6,8,EndOfPeriod,NotApplicable,NotApplicable,NotApplicable,EndOfPeriod,200,1900,1
EOP,C,4,6,14,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,220,1900,1";


var estimateType = 
@"@@EstimateType,,,,,,,,,,,
SystemName,DisplayName,Order,StructureType,InputSource,PeriodType,ExternalId0,ExternalId1,ExternalId2,ExternalId3,ExternalId4,,,,,,
BE,Best Estimate of Present Value,1,2,4,EndOfPeriod,,,,,,,
RA,Risk Adjustment,10,2,4,EndOfPeriod,,,,,,,
P,Patterns,15,2,4,EndOfPeriod,,,,,,,
C,Contractual Service Margin,20,8,7,NotApplicable,,,,,,,
L,Loss Component,30,8,7,NotApplicable,,,,,,,
LR,Loss Recovery Component,40,8,7,NotApplicable,,,,,,,
PL,Profit and Loss,50,1,7,NotApplicable,,,,,,,
AA,Advance Actuals,60,4,7,NotApplicable,PayablePR,ReceivableNIC,ReceivableICO,RiReceivablePR,RiPayableNIC,,,,,,
OA,Overdue Actuals,70,4,7,NotApplicable,ReceivablePR,PayableNIC,PayableICO,RiPayablePR,RiReceivableNIC,,,,,,
DA,Deferrable,80,8,7,NotApplicable,,,,,,,
R,PAA Revenues,85,8,4,BeginningOfPeriod,,,,,,,
A,Actuals,90,1,6,NotApplicable,,,,,,,
F,Factors,100,1,4,NotApplicable,,,,,,,
BEPA,Best Estimate of Present Value To Csm,110,1,4,NotApplicable,,,,,,,
APA,Actuals To Csm,120,1,6,NotApplicable,,,,,,,";


var economicBasis = 
@"@@EconomicBasis,,,,,,,,,,,
SystemName,DisplayName,Order
L,Locked-in,1
C,Current,10
N,Nominal,20";


var projectionConfiguration = 
@"@@ProjectionConfiguration,,,,,,,,,,,
SystemName,DisplayName,Shift,TimeStep,Order,,,,,,,
P0,End of January,0,1,10,,,,,,,
P1,End of February,0,2,20,,,,,,,
P2,End of March,0,3,30,,,,,,,
P3,End of April,0,4,40,,,,,,,
P4,End of May,0,5,50,,,,,,,
P5,End of June,0,6,60,,,,,,,
P6,End of July,0,7,70,,,,,,,
P7,End of August,0,8,80,,,,,,,
P8,End of September,0,9,90,,,,,,,
P9,End of October,0,10,100,,,,,,,
P10,End of November,0,11,110,,,,,,,
P11,End of December,0,12,120,,,,,,,
P12,End of Year+1,12,12,130,,,,,,,
P13,End of Year+2,24,12,140,,,,,,,
P14,End of Year+3,36,12,150,,,,,,,
P15,End of Year+4,48,12,160,,,,,,,
P16,Year+5 to Year+10,60,60,170,,,,,,,
P17,Year+10 to Year+15,120,60,180,,,,,,,
P18,Year+15 to Year+20,180,60,190,,,,,,,
P19,Years Over +20,240,9999,200,,,,,,,";


var amountType = 
@"@@AmountType
SystemName,DisplayName,Parent,Order,PeriodType
PR,Premiums,,10,BeginningOfPeriod
CL,Claims,,20,EndOfPeriod
NIC,Non Investment Component,CL,30,EndOfPeriod
ICO,Investment Component,CL,40,EndOfPeriod
CDR,Credit Default Risk,CL,50,EndOfPeriod
CE,Claim Expenses,CL,200,EndOfPeriod
ALE,Allocated Loss Adjustment Expenses,CE,210,EndOfPeriod
ULE,Unallocated Loss Adjustment Expenses,CE,220,EndOfPeriod
AE,Attributable Expenses,,80,BeginningOfPeriod
AEA,Aquisition,AE,90,BeginningOfPeriod
AEM,Maintenance,AE,100,BeginningOfPeriod
NE,Non Attributable Expenses,,110,BeginningOfPeriod
AC,Attributable Commission,,120,BeginningOfPeriod
ACA,Aquisition,AC,130,BeginningOfPeriod
ACM,Maitenance,AC,140,BeginningOfPeriod
DE,Deferrals,,200,EndOfPeriod
DAE,Aquisition Expenses,DE,220,EndOfPeriod
CU,Coverage Units,,150,EndOfPeriod";


var reportingNodes = new ReportingNode[] {
    new ReportingNode() {SystemName = "CH",DisplayName = "Switzerland",Currency = "CHF"},
    new ReportingNode() {SystemName = "G",DisplayName = "Group",Currency = "CHF"},
};


var groupOfInsuranceContracts = "DT1.1";
var groupOfReinsuranceContracts = "DTR1.1";
var reportingNode = "CH";
var scenarioBestEstimate = (string)null;
var scenarioMortalityUp = "MTUP";
var scenarioMortalityDown = "MTDOWN";


var args = new ImportArgs(reportingNode, 2021, 3, Periodicity.Quarterly, scenarioBestEstimate, ImportFormats.Actual);
var previousArgs = new ImportArgs(reportingNode, 2020, 12, Periodicity.Quarterly, scenarioBestEstimate, ImportFormats.Actual);
var argsScenarioMTUP = new ImportArgs(reportingNode, 2021, 3, Periodicity.Quarterly, scenarioMortalityUp, ImportFormats.Actual);
var previousScenarioArgsMTUP = new ImportArgs(reportingNode, 2020, 12, Periodicity.Quarterly, scenarioMortalityUp, ImportFormats.Actual);
var argsScenarioMTDOWN = new ImportArgs(reportingNode, 2021, 3, Periodicity.Quarterly, scenarioMortalityDown, ImportFormats.Actual);
var previousScenarioArgsMTDOWN = new ImportArgs(reportingNode, 2020, 12, Periodicity.Quarterly, scenarioMortalityDown, ImportFormats.Actual);


var partitionReportingNode = new PartitionByReportingNode { Id = (Guid)(await DataSource.Partition.GetKeyForInstanceAsync<PartitionByReportingNode>(args)),
                                                            ReportingNode = args.ReportingNode};


var partition = new PartitionByReportingNodeAndPeriod { Id = (Guid)(await DataSource.Partition.GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(args)),
                                                        ReportingNode = reportingNode, 
                                                        Scenario = scenarioBestEstimate, 
                                                        Year = args.Year,
                                                        Month = args.Month };
var previousPeriodPartition = new PartitionByReportingNodeAndPeriod { Id = (Guid)(await DataSource.Partition.GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(previousArgs)),
                                                                      ReportingNode = reportingNode, 
                                                                      Scenario = scenarioBestEstimate, 
                                                                      Year = previousArgs.Year,
                                                                      Month = previousArgs.Month };
var partitionScenarioMTUP = new PartitionByReportingNodeAndPeriod { Id = (Guid)(await DataSource.Partition.GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(argsScenarioMTUP)),
                                                                    ReportingNode = reportingNode, 
                                                                    Scenario = scenarioMortalityUp, 
                                                                    Year = args.Year,
                                                                    Month = args.Month };
var previousPeriodPartitionScenarioMTUP = new PartitionByReportingNodeAndPeriod { Id = (Guid)(await DataSource.Partition.GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(previousScenarioArgsMTUP)),
                                                                                  ReportingNode = reportingNode, 
                                                                                  Scenario = scenarioMortalityUp, 
                                                                                  Year = previousScenarioArgsMTUP.Year,
                                                                                  Month = previousScenarioArgsMTUP.Month };
var partitionScenarioMTDOWN = new PartitionByReportingNodeAndPeriod { Id = (Guid)(await DataSource.Partition.GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(argsScenarioMTUP)),
                                                                    ReportingNode = reportingNode, 
                                                                    Scenario = scenarioMortalityDown, 
                                                                    Year = args.Year,
                                                                    Month = args.Month };
var previousPeriodPartitionScenarioMTDOWN = new PartitionByReportingNodeAndPeriod { Id = (Guid)(await DataSource.Partition.GetKeyForInstanceAsync<PartitionByReportingNodeAndPeriod>(previousScenarioArgsMTUP)),
                                                                                    ReportingNode = reportingNode, 
                                                                                    Scenario = scenarioMortalityDown, 
                                                                                    Year = previousScenarioArgsMTDOWN.Year,
                                                                                    Month = previousScenarioArgsMTDOWN.Month };


var dt1 = new Portfolio(){
    Partition = partitionReportingNode.Id,
    ContractualCurrency = "USD",
    LineOfBusiness = "ANN",
    ValuationApproach ="BBA",
    OciType = "Default",
    SystemName = "DT1",
    DisplayName = "DT1 OCI"
};

var dtr1 = new Portfolio(){
    Partition = partitionReportingNode.Id,
    ContractualCurrency = "USD",
    LineOfBusiness = "ANN",
    ValuationApproach ="BBA",
    OciType = "Default",
    SystemName = "DTR1",
    DisplayName = "DTR1 OCI"
};

var dt11 = new GroupOfInsuranceContract(){
    Portfolio = "DT1",
    Profitability = "P",
    LiabilityType = "LRC",
    AnnualCohort = 2020,
    Partition = partitionReportingNode.Id,
    ContractualCurrency =  "USD",
    LineOfBusiness = "ANN",
    ValuationApproach = "BBA",
    OciType ="Default" ,
    SystemName = "DT1.1",
    DisplayName = "DT1.1 OCI LRC PA 0.8"
};

var dtr11 = new GroupOfReinsuranceContract(){
    Portfolio = "DTR1",
    Profitability = "P",
    LiabilityType = "LRC",
    AnnualCohort = 2020,
    Partition = partitionReportingNode.Id,
    ContractualCurrency =  "USD",
    LineOfBusiness = "ANN",
    ValuationApproach = "BBA",
    OciType ="Default" ,
    SystemName = "DTR1.1",
    DisplayName = "DTR1.1 OCI LRC PA 0.8"
};

var dt11State = new DataNodeState {
            DataNode = "DT1.1",
            State = State.Active,
            Year = previousArgs.Year,
            Month = previousArgs.Month,
            Partition = partitionReportingNode.Id
        };

var dtr11State = new DataNodeState {
            DataNode = "DTR1.1",
            State = State.Active,
            Year = previousArgs.Year,
            Month = previousArgs.Month,
            Partition = partitionReportingNode.Id
        };

var dt11SingleParameter = new SingleDataNodeParameter {
                                        Year = previousArgs.Year,
                                        Month = previousArgs.Month,
                                        DataNode = "DT1.1",
                                        PremiumAllocation = .8,
                                        Partition = partitionReportingNode.Id
                                    };

var dt11Inter = new InterDataNodeParameter{
    LinkedDataNode = "DTR1.1",
	ReinsuranceCoverage = 1,
	Year = args.Year,
	Month = args.Month,
	DataNode = "DT1.1",
	Scenario = args.Scenario,
    Partition = partitionReportingNode.Id
};


var yieldCurve = new YieldCurve(){
    Currency = "USD",
    Year = 2021,
    Month = 3,
    Values = new []{0.005,0.005,0.005,0.005}
};

var yieldCurvePrevious = new YieldCurve(){
    Currency = "USD",
    Year = 2020,
    Month = 12,
    Values = new []{0.002, 0.002, 0.002, 0.002}
};
