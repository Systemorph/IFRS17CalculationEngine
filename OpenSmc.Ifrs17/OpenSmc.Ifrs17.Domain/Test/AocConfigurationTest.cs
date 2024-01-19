#!import "../CalculationEngine"


string novelties = 
@"@@Novelty
SystemName,DisplayName,Parent,Order
I,In Force,,1
N,New Business,,10
C,Combined,,20";


await Import.FromString(novelties).WithType<Novelty>().WithTarget(DataSource).ExecuteAsync()


var workspace = Workspace.CreateNew();
workspace.InitializeFrom(DataSource);


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
CL,Combined Liabilities,,130,,,,,,,,
EA,Experience Adjustment,,140,,,,,,,,
AM,Amortization,,150,,,,,,,,
WO,Write-Off,,155,,,,,,,,
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
BOP,N,1,4,10,Average,EndOfPeriod,EndOfPeriod,BeginningOfPeriod,EndOfPeriod,95,1900,1
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


public async Task<ActivityLog> CheckAocConfigurationDefault(AocType[] newAocTypes)
{
    await workspace.DeleteAsync<AocType>( await workspace.Query<AocType>().ToArrayAsync() );
    await workspace.DeleteAsync<AocConfiguration>( await workspace.Query<AocConfiguration>().ToArrayAsync() );
    await workspace.CommitAsync();

    var aocTypeLog = await Import.FromString(canonicalAocTypes).WithType<AocType>().WithTarget(workspace).ExecuteAsync();
    aocTypeLog.Errors.Any().Should().Be(false);

    await workspace.UpdateAsync(newAocTypes);
    await workspace.CommitAsync();

    return await Import.FromString(canonicalAocConfig).WithFormat("AocConfiguration").WithTarget(workspace).ExecuteAsync();
}


var aocConfigLog = await CheckAocConfigurationDefault( new[]{ new AocType{SystemName = "A1", DisplayName = "a1", Order = 25} } );
aocConfigLog.Errors.Any().Should().Be(false);


var newConfigExpected = (await workspace.Query<AocConfiguration>().ToArrayAsync()).First(x => 
                         x.AocType == AocTypes.MC && x.Novelty == Novelties.I) with {AocType = "A1", Order = 21, DataType = DataType.Optional};
var newConfigCalculated = await workspace.Query<AocConfiguration>().Where(x => x == newConfigExpected).ToArrayAsync();
newConfigCalculated.Count().Should().Be(1);


var aocConfigLog = await CheckAocConfigurationDefault( new[]{ new AocType{SystemName = "A1", DisplayName = "a1", Order = 45} } );
aocConfigLog.Errors.Any().Should().Be(false);


var newConfigExpected = (await workspace.Query<AocConfiguration>().ToArrayAsync()).First(x => 
                         x.AocType == AocTypes.RCU && x.Novelty == Novelties.I) with {AocType = "A1", Order = 31, DataType = DataType.Optional};
var newConfigCalculated = await workspace.Query<AocConfiguration>().Where(x => x == newConfigExpected).ToArrayAsync();
newConfigCalculated.Count().Should().Be(1);


var aocConfigLog = await CheckAocConfigurationDefault( new[]{ new AocType{SystemName = "A1", DisplayName = "a1", Order = 82} } );
aocConfigLog.Errors.Any().Should().Be(false);


var newConfigExpected = (await workspace.Query<AocConfiguration>().ToArrayAsync()).First(x => 
                         x.AocType == AocTypes.AU && x.Novelty == Novelties.I) with {AocType = "A1", Order = 61, DataType = DataType.Optional};
var newConfigCalculated = await workspace.Query<AocConfiguration>().Where(x => x == newConfigExpected).ToArrayAsync();
newConfigCalculated.Count().Should().Be(1);


var newConfigExpected = (await workspace.Query<AocConfiguration>().ToArrayAsync()).First(x => 
                         x.AocType == AocTypes.AU && x.Novelty == Novelties.N) with {AocType = "A1", Order = 131, DataType = DataType.Optional};
var newConfigCalculated = await workspace.Query<AocConfiguration>().Where(x => x == newConfigExpected).ToArrayAsync();
newConfigCalculated.Count().Should().Be(1);


var aocConfigLog = await CheckAocConfigurationDefault( new[]{ new AocType{SystemName = "A1", DisplayName = "a1", Order = 106} } );
aocConfigLog.Errors.Any().Should().Be(false);


var newConfigExpected = (await workspace.Query<AocConfiguration>().ToArrayAsync()).First(x => 
                         x.AocType == AocTypes.EV && x.Novelty == Novelties.I) with {AocType = "A1", Order = 81};
var newConfigCalculated = await workspace.Query<AocConfiguration>().Where(x => x == newConfigExpected).ToArrayAsync();
newConfigCalculated.Count().Should().Be(1);


var newConfigExpected = (await workspace.Query<AocConfiguration>().ToArrayAsync()).First(x => 
                         x.AocType == AocTypes.EV && x.Novelty == Novelties.N) with {AocType = "A1", Order = 131};
var newConfigCalculated = await workspace.Query<AocConfiguration>().Where(x => x == newConfigExpected).ToArrayAsync();
newConfigCalculated.Count().Should().Be(1);


var aocConfigLog = await CheckAocConfigurationDefault( new[]{ new AocType{SystemName = "A1", DisplayName = "a1", Order = 116} } );
aocConfigLog.Errors.Any().Should().Be(false);


var newConfigExpected = (await workspace.Query<AocConfiguration>().ToArrayAsync()).First(x => 
                         x.AocType == AocTypes.EV && x.Novelty == Novelties.I) with {AocType = "A1", Order = 91};
var newConfigCalculated = await workspace.Query<AocConfiguration>().Where(x => x == newConfigExpected).ToArrayAsync();
newConfigCalculated.Count().Should().Be(1);


var newConfigExpected = (await workspace.Query<AocConfiguration>().ToArrayAsync()).First(x => 
                         x.AocType == AocTypes.EV && x.Novelty == Novelties.N) with {AocType = "A1", Order = 141};
var newConfigCalculated = await workspace.Query<AocConfiguration>().Where(x => x == newConfigExpected).ToArrayAsync();
newConfigCalculated.Count().Should().Be(1);


var aocConfigLog = await CheckAocConfigurationDefault( new[]{ new AocType{SystemName = "A1", DisplayName = "a1", Order = 152} } );
aocConfigLog.Errors.Any().Should().Be(false);


var newConfigExpected = (await workspace.Query<AocConfiguration>().ToArrayAsync()).First(x => 
                         x.AocType == AocTypes.WO && x.Novelty == Novelties.C) with {AocType = "A1", Order = 196};
var newConfigCalculated = await workspace.Query<AocConfiguration>().Where(x => x == newConfigExpected).ToArrayAsync();


newConfigCalculated.Count().Should().Be(1);


var aocConfigLog = await CheckAocConfigurationDefault( new[]{ new AocType{SystemName = "A1", DisplayName = "a1", Order = 111},
                                                              new AocType{SystemName = "A2", DisplayName = "a1", Order = 112},
                                                              new AocType{SystemName = "A3", DisplayName = "a1", Order = 113},
                                                              new AocType{SystemName = "A4", DisplayName = "a1", Order = 114},
                                                              new AocType{SystemName = "A5", DisplayName = "a1", Order = 115},
                                                               } );



(aocConfigLog.Errors.First().ToString() == "ActivityMessageNotification { Message = Two or more AoC Configurations have the same Order. }")
.Should().Be(true);


workspace.Dispose()



