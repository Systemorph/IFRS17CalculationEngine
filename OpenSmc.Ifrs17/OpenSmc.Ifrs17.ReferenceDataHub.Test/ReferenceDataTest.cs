using System.IO.Enumeration;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using OpenSmc.Activities;
using OpenSmc.Data;
using OpenSmc.Hub.Fixture;
using OpenSmc.Import;
using OpenSmc.Messaging;
using Xunit;
using Xunit.Abstractions;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.DataTypes.Constants;

namespace OpenSmc.Ifrs17.ReferenceDataHub.Test;

public class ImportReferenceDataTest(ITestOutputHelper output) : HubTestBase(output)
    {
        public static readonly Dictionary<Type, IEnumerable<object>> ReferenceDataDomain
            =
            new()
            {
                { typeof(AmountType), new[] { new AmountType( "PR", "WrongPremiums", "", 10, PeriodType.BeginningOfPeriod ) } },
                { typeof(DeferrableAmountType), new DeferrableAmountType[] { new("DE", "WrongDeferrals", "", 10, PeriodType.BeginningOfPeriod) } },
                { typeof(AocType), new AocType[] { new() { SystemName = "BoP", DisplayName = "BoP", Parent = "", Order = 10 } } },
                { typeof(AocConfiguration), new AocConfiguration[] { new()
                {
                    AocType = "BoP", Novelty = "N", DataType = DataTypes.Constants.Enumerates.DataType.CalculatedProjection|DataTypes.Constants.Enumerates.DataType.Optional , InputSource = InputSource.Cashflow|InputSource.Actual|InputSource.Opening,
                    StructureType = StructureType.AocPvAcTm, FxPeriod = FxPeriod.BeginningOfPeriod,
                    YcPeriod = PeriodType.BeginningOfPeriod, CdrPeriod = PeriodType.BeginningOfPeriod,
                    ValuationPeriod = ValuationPeriod.BeginningOfPeriod, RcPeriod = PeriodType.BeginningOfPeriod, Order = 10, Year = 1900, Month = 1
                } } }
            };

        protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration)
        {

            return base.ConfigureHost(configuration)
                .AddData(data => data.WithDataSource
                (
                    nameof(DataSource),
                    source => source.ConfigureCategory(ReferenceDataDomain)
                ))
                .AddImport(import => import);
        }

        [Fact]
        public async Task ImportDimensionTest()
        {
            var client = GetClient();
            var importRequest = new ImportRequest(DimensionsCsv);
            var importResponse = await client.AwaitResponse(importRequest, o => o.WithTarget(new HostAddress()));
            importResponse.Message.Log.Status.Should().Be(ActivityLogStatus.Succeeded);

            var atItems = await client.AwaitResponse(new GetManyRequest<AmountType>(),
                o => o.WithTarget(new HostAddress()));
            var datItems = await client.AwaitResponse(new GetManyRequest<DeferrableAmountType>(),
                o => o.WithTarget(new HostAddress()));
            var aocItems = await client.AwaitResponse(new GetManyRequest<AocType>(),
                o => o.WithTarget(new HostAddress()));
            var aoccItems = await client.AwaitResponse(new GetManyRequest<AocConfiguration>(),
                o => o.WithTarget(new HostAddress()));

            atItems.Message.Items.Should().HaveCount(17)
                .And.ContainSingle(i => i.SystemName == "PR")
                .Which.DisplayName.Should().Be("Premiums"); // we started with WrongPremiums....
            atItems.Message.Items.Should().ContainSingle(i => i.SystemName == "NIC").Which.Parent.Should().Be("CL");
            
            datItems.Message.Items.Should().HaveCount(2)
                .And.ContainSingle(i => i.SystemName == "DE")
                .Which.DisplayName.Should().Be("Deferrals");

            aocItems.Message.Items.Should().HaveCount(18);

            aoccItems.Message.Items.Should().HaveCount(20);
        }

        private const string DimensionsCsv =
            @"@@AmountType,,,,,,,,,,,,
SystemName,DisplayName,Parent,Order,PeriodType,,,,,,,,
PR,Premiums,,10,BeginningOfPeriod,,,,,,,,
CL,Claims,,20,EndOfPeriod,,,,,,,,
NIC,Non Investment Component,CL,30,EndOfPeriod,,,,,,,,
ICO,Investment Component,CL,40,EndOfPeriod,,,,,,,,
CDR,Credit Default Risk,CL,50,EndOfPeriod,,,,,,,,
CDRI,Initial Credit Default Risk,CDR,60,EndOfPeriod,,,,,,,,
CE,Claim Expenses,CL,200,EndOfPeriod,,,,,,,,
ALE,Allocated Loss Adjustment Expenses,CE,210,EndOfPeriod,,,,,,,,
ULE,Unallocated Loss Adjustment Expenses,CE,220,EndOfPeriod,,,,,,,,
AE,Attributable Expenses,,80,EndOfPeriod,,,,,,,,
AEA,Aquisition,AE,90,BeginningOfPeriod,,,,,,,,
AEM,Maintenance,AE,100,EndOfPeriod,,,,,,,,
NE,Non Attributable Expenses,,110,BeginningOfPeriod,,,,,,,,
AC,Attributable Commission,,120,BeginningOfPeriod,,,,,,,,
ACA,Aquisition,AC,130,BeginningOfPeriod,,,,,,,,
ACM,Maitenance,AC,140,EndOfPeriod,,,,,,,,
CU,Coverage Units,,150,EndOfPeriod,,,,,,,,
@@DeferrableAmountType,,,,,,,,,,,,
SystemName,DisplayName,Parent,Order,PeriodType,,,,,,,,
DE,Deferrals,,10,BeginningOfPeriod,,,,,,,,
DAE,Aquisition Expenses,DE,20,BeginningOfPeriod,,,,,,,,
@@AocType,,,,,,,,,,,,
SystemName,DisplayName,Parent,Order,,,,,,,,,
BOP,Opening Balance,,10,,,,,,,,,
MC,Model Correction,,20,,,,,,,,,
PC,Portfolio Changes,,30,,,,,,,,,
RCU,Reinsurance Coverage Update,PC,40,,,,,,,,,
CF,Cash flow,,50,,,,,,,,,
IA,Interest Accretion,,60,,,,,,,,,
AU,Assumption Update,,70,,,,,,,,,
FAU,Financial Assumption Update,,80,,,,,,,,,
YCU,Yield Curve Update,FAU,90,,,,,,,,,
CRU,Credit Risk Update,FAU,100,,,,,,,,,
EV,Experience Variance,,110,,,,,,,,,
WO,Write-Off,,120,,,,,,,,,
CL,Combined Liabilities,,130,,,,,,,,,
EA,Experience Adjustment,,140,,,,,,,,,
AM,Amortization,,150,,,,,,,,,
FX,FX Impact,,160,,,,,,,,,
EOP,Closing Balance,,170,,,,,,,,,
@@AocConfiguration,,,,,,,,,,,,
AocType,Novelty,DataType,InputSource,StructureType,FxPeriod,YcPeriod,CdrPeriod,ValuationPeriod,RcPeriod,Order,Year,Month
BOP,I,17,7,PV|AC|TM,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,10,1900,1
MC,I,1,4,PV|TM,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,20,1900,1
RCU,I,4,4,TM,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,BeginningOfPeriod,EndOfPeriod,30,1900,1
CF,I,20,4,PV,Average,NotApplicable,BeginningOfPeriod,Delta,EndOfPeriod,40,1900,1
IA,I,20,5,PV|TM,Average,BeginningOfPeriod,BeginningOfPeriod,Delta,EndOfPeriod,50,1900,1
AU,I,1,4,PV|TM,EndOfPeriod,BeginningOfPeriod,BeginningOfPeriod,EndOfPeriod,EndOfPeriod,60,1900,1
YCU,I,8,4,PV|TM,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,EndOfPeriod,70,1900,1
CRU,I,8,4,PV|TM,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,80,1900,1
EV,I,1,4,PV|TM,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,90,1900,1
BOP,N,1,4,PV|TM,Average,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,100,1900,1
MC,N,1,4,PV|TM,Average,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,105,1900,1
CF,N,4,4,PV,Average,NotApplicable,EndOfPeriod,Delta,EndOfPeriod,110,1900,1
IA,N,4,4,PV|TM,Average,EndOfPeriod,EndOfPeriod,Delta,EndOfPeriod,120,1900,1
AU,N,1,4,PV|TM,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,130,1900,1
EV,N,1,4,PV|TM,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,140,1900,1
CL,C,2,4,PV|TM,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,180,1900,1
EA,C,4,4,TM,EndOfPeriod,NotApplicable,NotApplicable,NotApplicable,EndOfPeriod,190,1900,1
CF,C,17,6,AC,Average,NotApplicable,NotApplicable,NotApplicable,NotApplicable,193,1900,1
WO,C,17,2,AC,Average,NotApplicable,NotApplicable,NotApplicable,NotApplicable,195,1900,1
AM,C,4,6,TM,EndOfPeriod,NotApplicable,NotApplicable,NotApplicable,EndOfPeriod,200,1900,1
EOP,C,4,6,PV|AC|TM,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,EndOfPeriod,220,1900,1
";

        private const string NotImported =
            @"
,,,,,,,,,,,,
@@CreditRiskRating,,,,,,,,,,,,
SystemName,DisplayName,,,,,,,,,,,
AAA,AAA,,,,,,,,,,,
AA+,AA+,,,,,,,,,,,
AA,AA,,,,,,,,,,,
AA-,AA-,,,,,,,,,,,
A+,A+,,,,,,,,,,,
A,A,,,,,,,,,,,
A-,A-,,,,,,,,,,,
BBB+,BBB+,,,,,,,,,,,
BBB,BBB,,,,,,,,,,,
BBB-,BBB-,,,,,,,,,,,
BB+,BB+,,,,,,,,,,,
BB,BB,,,,,,,,,,,
BB-,BB-,,,,,,,,,,,
B+,B+,,,,,,,,,,,
B,B,,,,,,,,,,,
B-,B-,,,,,,,,,,,
CCC+,CCC+,,,,,,,,,,,
CCC,CCC,,,,,,,,,,,
CCC-,CCC-,,,,,,,,,,,
CC,CC,,,,,,,,,,,
C,C,,,,,,,,,,,
I,I,,,,,,,,,,,
,,,,,,,,,,,,
@@Currency,,,,,,,,,,,,
SystemName,DisplayName,,,,,,,,,,,
USD,United States Dollar,,,,,,,,,,,
CHF,Swiss Franc,,,,,,,,,,,
DKK,Danish Krone,,,,,,,,,,,
EUR,Euro,,,,,,,,,,,
GBP,British Pound,,,,,,,,,,,
HKD,Hong Kong Dollar,,,,,,,,,,,
ITL,Italian Lira,,,,,,,,,,,
PLN,Polish Zloty,,,,,,,,,,,
SKK,Slovakian Krona,,,,,,,,,,,
XTSHY,Testing Currency (High Yields),,,,,,,,,,,
,,,,,,,,,,,,
@@EconomicBasis,,,,,,,,,,,,
SystemName,DisplayName,Order,,,,,,,,,,
N,Nominal,1,,,,,,,,,,
L,Locked-in,10,,,,,,,,,,
C,Current,20,,,,,,,,,,
,,,,,,,,,,,,
@@EstimateType,,,,,,,,,,,,
SystemName,DisplayName,Order,StructureType,InputSource,PeriodType,,,,,,,
BE,Best Estimate of Present Value,1,PV,4,EndOfPeriod,,,,,,,
RA,Risk Adjustment,10,PV,4,EndOfPeriod,,,,,,,
P,Patterns,15,PV,4,EndOfPeriod,,,,,,,
C,Contractual Service Margin,20,TM,7,NotApplicable,,,,,,,
L,Loss Component,30,TM,7,NotApplicable,,,,,,,
LR,Loss Recovery Component,40,TM,7,NotApplicable,,,,,,,
PL,Profit and Loss,50,NO,7,NotApplicable,,,,,,,
AA,Advance Actuals,60,AC,7,NotApplicable,,,,,,,
OA,Overdue Actuals,70,AC,7,NotApplicable,,,,,,,
DA,Deferrable,80,TM,7,NotApplicable,,,,,,,
R,PAA Revenues,85,TM,4,BeginningOfPeriod,,,,,,,
A,Actuals,90,NO,6,NotApplicable,,,,,,,
F,Factors,100,NO,4,NotApplicable,,,,,,,
BEPA,Best Estimate of Present Value To Csm,110,NO,4,NotApplicable,,,,,,,
APA,Actuals To Csm,120,NO,6,NotApplicable,,,,,,,
,,,,,,,,,,,,
@@LiabilityType,,,,,,,,,,,,
SystemName,DisplayName,,,,,,,,,,,
LRC,Liability for Remaining Coverage,,,,,,,,,,,
LIC,Liabilities for Incurred Claims,,,,,,,,,,,
,,,,,,,,,,,,
@@LineOfBusiness,,,,,,,,,,,,
SystemName,DisplayName,Parent,,,,,,,,,,
M,Multiline Life and Non-Life,,,,,,,,,,,
LI,Life,M,,,,,,,,,,
NL,Non-Life,M,,,,,,,,,,
LIA,Liability,NL,,,,,,,,,,
MAE,""Marine, Aviation & Energy"",NL,,,,,,,,,,
MOT,Motor,NL,,,,,,,,,,
NAH,Non-Life Accident & Health,NL,,,,,,,,,,
PEA,""Property, Engineering & Agriculture"",NL,,,,,,,,,,
ONL,Other Non-Life,NL,,,,,,,,,,
ANN,Annuity,LI,,,,,,,,,,
DIS,Disability,LI,,,,,,,,,,
END,Endowment,LI,,,,,,,,,,
HYB,Hybrid,LI,,,,,,,,,,
ULI,Unit Linked,LI,,,,,,,,,,
OLI,Other Life,LI,,,,,,,,,,
,,,,,,,,,,,,
@@Novelty,,,,,,,,,,,,
SystemName,DisplayName,Order,,,,,,,,,,
I,In Force,1,,,,,,,,,,
N,New Business,10,,,,,,,,,,
C,Combined,20,,,,,,,,,,
,,,,,,,,,,,,
@@OciType,,,,,,,,,,,,
SystemName,DisplayName,,,,,,,,,,,
Default,Default,,,,,,,,,,,
,,,,,,,,,,,,
@@Partner,,,,,,,,,,,,
SystemName,DisplayName,,,,,,,,,,,
PT1,Partner1,,,,,,,,,,,
PTI,Internal Partner,,,,,,,,,,,
,,,,,,,,,,,,
@@BsVariableType,,,,,,,,,,,,
SystemName,DisplayName,Parent,Order,,,,,,,,,
D,Changes in Balance,,10,,,,,,,,,
,,,,,,,,,,,,
@@PnlVariableType,,,,,,,,,,,,
SystemName,DisplayName,Parent,Order,,,,,,,,,
TCI,Total Comprehensive Income,,0,,,,,,,,,
PNL,Profit and Loss,TCI,100,,,,,,,,,
OCI,Other Comprehensive Income,TCI,200,,,,,,,,,
ISR,Insurance Service Result,PNL,300,,,,,,,,,
IR,Insurance Revenue,ISR,400,,,,,,,,,
ISE,Insurance Service Expense,ISR,500,,,,,,,,,
IFIE,Insurance Finance Income/Expense,PNL,600,,,,,,,,,
IR1,Premiums,IR,401,,,,,,,,,
IR2,Exc. Investment Components,IR,402,,,,,,,,,
IR3,CSM Amortization,IR,403,,,,,,,,,
IR4,Acquistion Expenses Amortization,IR,404,,,,,,,,,
IR5,Non-Financial LRC/LC Changes (Exc. CSM Amortization),IR,405,,,,,,,,,
IR6,Exc. Experience Adjustments,IR,406,,,,,,,,,
IR7,On Premiums,IR6,407,,,,,,,,,
IR77,Total,IR7,408,,,,,,,,,
IR78,To CSM,IR7,409,,,,,,,,,
IR79,To Financial Performance,IR7,410,,,,,,,,,
IR8,On Acquistion Expenses,IR6,411,,,,,,,,,
IR9,Exc. Financial on Deferrals,IR,415,,,,,,,,,
IR11,PAA Earned Premiums,IR1,451,,,,,,,,,
IR12,Experience Adjustment on Premiums,IR1,452,,,,,,,,,
IR13,Expected Releases / Amortizations,IR,453,,,,,,,,,
IR14,FCF Locked-In Interest Rate Correction,IR,454,,,,,,,,,
ISE1,Reinsurance Premiums,ISE,501,,,,,,,,,
ISE2,Claims,ISE,502,,,,,,,,,
ISE3,Expenses,ISE,503,,,,,,,,,
ISE4,Commissions,ISE,504,,,,,,,,,
ISE41,Claim Expenses,ISE,505,,,,,,,,,
ISE5,Exc. Investment Components,ISE,506,,,,,,,,,
ISE6,Acquisition Expenses,ISE,507,,,,,,,,,
ISE7,Reinsurance CSM Amortization,ISE,508,,,,,,,,,
ISE8,LoReCo Release,ISE,509,,,,,,,,,
ISE9,Loss Component Release,ISE,510,,,,,,,,,
ISE10,Non-Financial Reinsurance LRC Changes (Exc. LC/LoReCo),ISE,511,,,,,,,,,
ISE11,Loss Component / LoReCo Changes (Exc. Releases),ISE,512,,,,,,,,,
ISE12,Non Financial LIC Changes,ISE,513,,,,,,,,,
ISE13,Exc. Financial on Deferrals,ISE,515,,,,,,,,,
ISE20,Reinsurance Revenue,ISE,520,,,,,,,,,
ISE201,PAA Earned Premiums,ISE20,521,,,,,,,,,
ISE21,Experience Adjustment on Premiums,ISE20,522,,,,,,,,,
ISE22,Expected Releases / Amortizations (Exc. LoReCo),ISE20,523,,,,,,,,,
ISE23,Exc. Investment Components,ISE20,524,,,,,,,,,
ISE24,FCF Locked-In Interest Rate Correction,ISE20,525,,,,,,,,,
ISE25,Exc. Financial on Deferrals,ISE20,526,,,,,,,,,
IFIE1,Financial LRC/LC Changes,IFIE,601,,,,,,,,,
IFIE2,Financial LIC Changes,IFIE,602,,,,,,,,,
IFIE3,FX Changes,IFIE,603,,,,,,,,,
OCI1,Financial LRC Changes,OCI,201,,,,,,,,,
OCI2,Financial LIC Changes,OCI,202,,,,,,,,,
OCI3,FX Changes,OCI,203,,,,,,,,,
,,,,,,,,,,,,
@@Profitability,,,,,,,,,,,,
SystemName,DisplayName,,,,,,,,,,,
O,Onerous,,,,,,,,,,,
P,Profitabile,,,,,,,,,,,
U,Undetermined,,,,,,,,,,,
,,,,,,,,,,,,
@@RiskDriver,,,,,,,,,,,,
SystemName,DisplayName,,,,,,,,,,,
Default,Default,,,,,,,,,,,
,,,,,,,,,,,,
@@Scenario,,,,,,,,,,,,
SystemName,DisplayName,,,,,,,,,,,
YCUP1.0pct,Yield Curve Up 1.0pct,,,,,,,,,,,
YCDW1.0pct,Yield Curve Down 1.0pct,,,,,,,,,,,
SRUP1.0pct,Spread Rate Up 1.0pct,,,,,,,,,,,
SRDW1.0pct,Spread Rate Down 1.0pct,,,,,,,,,,,
EUP1.0pct,Equity Up 1.0pct,,,,,,,,,,,
EDW1.0pct,Equity Down 1.0pct,,,,,,,,,,,
FXUP1.0pct,Exchange Rate Up 1.0pct,,,,,,,,,,,
FXDW1.0pct,Exchange Rate Down 1.0pct,,,,,,,,,,,
MTUP10pct,Mortality Up 10pct,,,,,,,,,,,
MTDW10pct,Mortality Down 10pct,,,,,,,,,,,
LUP10pct,Longevity Up 10pct,,,,,,,,,,,
LDW10pct,Longevity Down 10pct,,,,,,,,,,,
DUP10pct,Disability Up 10pct,,,,,,,,,,,
DDW10pct,Disability Down 10pct,,,,,,,,,,,
LICUP10pct,Lic Up 10pct,,,,,,,,,,,
LICDW10pct,Lic Down 10pct,,,,,,,,,,,
,,,,,,,,,,,,
@@ValuationApproach,,,,,,,,,,,,
SystemName,DisplayName,,,,,,,,,,,
BBA,Building Block Approach,,,,,,,,,,,
PAA,Premium Allocation Approach,,,,,,,,,,,
,,,,,,,,,,,,
@@ProjectionConfiguration,,,,,,,,,,,,
SystemName,DisplayName,Shift,TimeStep,Order,,,,,,,,
P0,End of January,0,1,10,,,,,,,,
P1,End of February,0,2,20,,,,,,,,
P2,End of March,0,3,30,,,,,,,,
P3,End of April,0,4,40,,,,,,,,
P4,End of May,0,5,50,,,,,,,,
P5,End of June,0,6,60,,,,,,,,
P6,End of July,0,7,70,,,,,,,,
P7,End of August,0,8,80,,,,,,,,
P8,End of September,0,9,90,,,,,,,,
P9,End of October,0,10,100,,,,,,,,
P10,End of November,0,11,110,,,,,,,,
P11,End of December,0,12,120,,,,,,,,
P12,End of Year+1,12,12,130,,,,,,,,
P13,End of Year+2,24,12,140,,,,,,,,
P14,End of Year+3,36,12,150,,,,,,,,
P15,End of Year+4,48,12,160,,,,,,,,
P16,Year+5 to Year+10,60,60,170,,,,,,,,
P17,Year+10 to Year+15,120,60,180,,,,,,,,
P18,Year+15 to Year+20,180,60,190,,,,,,,,
P19,Years Over +20,240,9999,200,,,,,,,,
";
    
}