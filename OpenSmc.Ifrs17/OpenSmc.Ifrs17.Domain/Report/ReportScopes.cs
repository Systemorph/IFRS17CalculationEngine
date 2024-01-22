using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;
using AmountTypes = OpenSmc.Ifrs17.Domain.Constants.AmountTypes;
using AocTypes = OpenSmc.Ifrs17.Domain.Constants.AocTypes;
using EconomicBases = OpenSmc.Ifrs17.Domain.Constants.EconomicBases;
using EstimateTypes = OpenSmc.Ifrs17.Domain.Constants.EstimateTypes;
using FxPeriod = OpenSmc.Ifrs17.Domain.Constants.FxPeriod;
using LiabilityTypes = OpenSmc.Ifrs17.Domain.Constants.LiabilityTypes;
using Novelties = OpenSmc.Ifrs17.Domain.Constants.Novelties;
using ValuationApproaches = OpenSmc.Ifrs17.Domain.Constants.ValuationApproaches;

//#!import "ReportStorage"


public interface IUniverse: IScopeWithStorage<ReportStorage> {}


public interface Data: IScope<(ReportIdentity ReportIdentity, string EstimateType), ReportStorage>, IDataCube<ReportVariable> {
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<Data>(s => s.WithApplicability<DataWrittenActual>(x => x.GetStorage().EstimateTypesWithoutAoc.Contains(x.Identity.EstimateType)));
    
    protected IDataCube<ReportVariable> RawData => GetStorage().GetVariables(Identity.ReportIdentity, Identity.EstimateType);

    private IDataCube<ReportVariable> RawEops => RawData.Filter(("VariableType", AocTypes.EOP));
    private IDataCube<ReportVariable> NotEopsNotCls => RawData.Filter(("VariableType", "!EOP"),("VariableType", "!CL")); // TODO negation must be hardcoded (also to avoid string concatenation)
    
    private IDataCube<ReportVariable> CalculatedCl => (RawEops - NotEopsNotCls)
                                                        .AggregateOver(nameof(Novelty), nameof(VariableType))
                                                        .SelectToDataCube(x => Math.Abs(x.Value) >= Precision, x => x with { Novelty = Novelties.C, VariableType = AocTypes.CL });
    
    private IDataCube<ReportVariable> CalculatedEops => (NotEopsNotCls + CalculatedCl)
                                                        .AggregateOver(nameof(Novelty), nameof(VariableType))
                                                        .SelectToDataCube(x => x with { VariableType = AocTypes.EOP, Novelty = Novelties.C });
      
    IDataCube<ReportVariable> Data => NotEopsNotCls + CalculatedCl + CalculatedEops;
}
public interface DataWrittenActual: Data {
    IDataCube<ReportVariable> Data.Data => RawData;
}


public interface Fx: IScope<(string ContractualCurrency, string FunctionalCurrency, OpenSmc.Ifrs17.Domain.Constants.FxPeriod FxPeriod, (int, int) Period, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage> {   
    private double groupFxRate => Identity.CurrencyType switch {
            OpenSmc.Ifrs17.Domain.Constants.CurrencyType.Group => GetStorage().GetFx(Identity.Period, Identity.FunctionalCurrency, GroupCurrency, FxPeriod.Average),
            _ => 1
    };
    
    private double GetFunctionalFxRate(FxPeriod fxPeriod) => Identity.CurrencyType switch {
            OpenSmc.Ifrs17.Domain.Constants.CurrencyType.Contractual => 1,
            _ => GetStorage().GetFx(Identity.Period, Identity.ContractualCurrency, Identity.FunctionalCurrency, fxPeriod)
    };
    
    double Fx => GetFunctionalFxRate(Identity.FxPeriod) * groupFxRate;
}


public interface FxData: IScope<(ReportIdentity ReportIdentity, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType, string EstimateType), ReportStorage>, IDataCube<ReportVariable> {
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<FxData>(s => s.WithApplicability<FxDataWrittenActual>(x => x.GetStorage().EstimateTypesWithoutAoc.Contains(x.Identity.EstimateType)));
    
    protected IDataCube<ReportVariable> Data => GetScope<Data>((Identity.ReportIdentity, Identity.EstimateType)).Data
        .SelectToDataCube(x => Multiply( GetScope<Fx>((Identity.ReportIdentity.ContractualCurrency, 
                                                       Identity.ReportIdentity.FunctionalCurrency, 
                                                       GetStorage().GetFxPeriod(GetStorage().Args.Period, Identity.ReportIdentity.Projection, x.VariableType, x.Novelty),
                                                       (Identity.ReportIdentity.Year, Identity.ReportIdentity.Month),
                                                       Identity.CurrencyType)).Fx, x ) with { Currency = Identity.CurrencyType switch {
                                                                                                                    OpenSmc.Ifrs17.Domain.Constants.CurrencyType.Contractual => x.ContractualCurrency,
                                                                                                                    OpenSmc.Ifrs17.Domain.Constants.CurrencyType.Functional => x.FunctionalCurrency,
                                                                                                                    _ => GroupCurrency }});
    
    private IDataCube<ReportVariable> Eops => Data.Filter(("VariableType", AocTypes.EOP));
    private IDataCube<ReportVariable> NotEops => Data.Filter(("VariableType", "!EOP")); // TODO negation must be hardcoded (also to avoid string concatenation)
    
    private IDataCube<ReportVariable> Fx => (Eops - NotEops)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(x => Math.Abs(x.Value) >= Precision, x => x with { Novelty = Novelties.C, VariableType = AocTypes.FX });
    
    IDataCube<ReportVariable> FxData => Data + Fx;
}

public interface FxDataWrittenActual: FxData {
    IDataCube<ReportVariable> FxData.FxData => Data;
}


using System.Reflection;
public static T[] GetAllPublicConstantValues<T>(this Type type, 
                            IList<T> excludedTerms = null)
{
    var selection =  type
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(T))
        .Select(x => (T)x.GetRawConstantValue())
        .ToArray();
    if (excludedTerms == null)
        return selection;
    else 
        return selection.Where(x => !excludedTerms.Contains(x)).ToArray();
}


public interface BestEstimate: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> BestEstimate => Identity.Id switch {
            { ValuationApproach: ValuationApproaches.PAA, LiabilityType: LiabilityTypes.LRC } => GetScope<LockedBestEstimate>(Identity).LockedBestEstimate, //TODO we should use the economic basis driver to decide which Economic basis to use
            { ValuationApproach: ValuationApproaches.BBA, IsOci: true } => GetScope<LockedBestEstimate>(Identity).LockedBestEstimate,
            _ => GetScope<CurrentBestEstimate>(Identity).CurrentBestEstimate };
}

public interface LockedBestEstimate: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> LockedBestEstimate => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.BE)).FxData
        .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.L), ("AmountType", "!CDRI"));
}

public interface CurrentBestEstimate: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> CurrentBestEstimate => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.BE)).FxData
        .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.C), ("AmountType", "!CDRI"));
}

public interface NominalBestEstimate: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> NominalBestEstimate => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.BE)).FxData
        .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.N), ("AmountType", "!CDRI"));
}


public interface RiskAdjustment: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> RiskAdjustment => Identity.Id switch {
            { ValuationApproach: ValuationApproaches.PAA, LiabilityType: LiabilityTypes.LRC } => GetScope<LockedRiskAdjustment>(Identity).LockedRiskAdjustment, //TODO we should use the economic basis driver to decide which Economic basis to use
            { ValuationApproach: ValuationApproaches.BBA, IsOci: true } => GetScope<LockedRiskAdjustment>(Identity).LockedRiskAdjustment,
            _ => GetScope<CurrentRiskAdjustment>(Identity).CurrentRiskAdjustment };
}

public interface LockedRiskAdjustment: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> LockedRiskAdjustment => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.RA)).FxData
        .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.L));
}

public interface CurrentRiskAdjustment: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> CurrentRiskAdjustment =>  GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.RA)).FxData
        .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.C));
}

public interface NominalRiskAdjustment: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> NominalRiskAdjustment => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.RA)).FxData
        .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.N));
}


public interface Fcf: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    private IDataCube<ReportVariable> BestEstimate => GetScope<BestEstimate>(Identity).BestEstimate;
    private IDataCube<ReportVariable> RiskAdjustment => GetScope<RiskAdjustment>(Identity).RiskAdjustment;
    
    IDataCube<ReportVariable> Fcf => BestEstimate + RiskAdjustment;
}

public interface CurrentFcf: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {  
    private IDataCube<ReportVariable> BestEstimate => GetScope<CurrentBestEstimate>(Identity).CurrentBestEstimate;
    private IDataCube<ReportVariable> RiskAdjustment => GetScope<CurrentRiskAdjustment>(Identity).CurrentRiskAdjustment;
    
    IDataCube<ReportVariable> CurrentFcf => BestEstimate + RiskAdjustment;
}

public interface LockedFcf: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {   
    private IDataCube<ReportVariable> BestEstimate => GetScope<LockedBestEstimate>(Identity).LockedBestEstimate;
    private IDataCube<ReportVariable> RiskAdjustment => GetScope<LockedRiskAdjustment>(Identity).LockedRiskAdjustment;
    
    IDataCube<ReportVariable> LockedFcf => BestEstimate + RiskAdjustment;
}

public interface NominalFcf: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {   
    private IDataCube<ReportVariable> BestEstimate => GetScope<NominalBestEstimate>(Identity).NominalBestEstimate;
    private IDataCube<ReportVariable> RiskAdjustment => GetScope<NominalRiskAdjustment>(Identity).NominalRiskAdjustment;
    
    IDataCube<ReportVariable> NominalFcf => BestEstimate + RiskAdjustment;
}


public interface Csm: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> Csm => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.C)).FxData;
}


public interface Lc: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> Lc => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.L)).FxData;
}


public interface Loreco: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> Loreco => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.LR)).FxData;
}


public interface LrcTechnicalMargin: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    private IDataCube<ReportVariable> Csm => GetScope<Csm>(Identity).Csm;
    private IDataCube<ReportVariable> Lc => GetScope<Lc>(Identity).Lc;
    private IDataCube<ReportVariable> Loreco => GetScope<Loreco>(Identity).Loreco;
    
    IDataCube<ReportVariable> LrcTechnicalMargin => Lc + Loreco - 1 * Csm;
}


public interface WrittenAndAccruals: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> Written => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.A)).FxData;
    IDataCube<ReportVariable> Advance => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.AA)).FxData;
    IDataCube<ReportVariable> Overdue => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.OA)).FxData;
}


public interface Deferrals: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> Deferrals => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.DA)).FxData;
}


public interface Revenues: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> Revenues => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.R)).FxData;
}


public interface ExperienceAdjustment: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    private IDataCube<ReportVariable> WrittenCashflow => GetScope<WrittenAndAccruals>(Identity).Written
        .Filter(("VariableType", AocTypes.CF));
    
    private IDataCube<ReportVariable> BestEstimateCashflow => GetScope<BestEstimate>(Identity).BestEstimate
        .Filter(("VariableType", AocTypes.CF), ("AmountType","!CDR"))
        .SelectToDataCube(rv => rv with { EconomicBasis = null, Novelty = Novelties.C });

    IDataCube<ReportVariable> ActuarialExperienceAdjustment => WrittenCashflow - BestEstimateCashflow;
}


public interface LicActuarial: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    IDataCube<ReportVariable> LicActuarial => GetScope<CurrentFcf>(Identity).CurrentFcf.Filter(("LiabilityType", LiabilityTypes.LIC));
}


public interface Lic: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    private IDataCube<ReportVariable> licActuarial => GetScope<LicActuarial>(Identity).LicActuarial;
    private IDataCube<ReportVariable> accrual => GetScope<WrittenAndAccruals>(Identity).Advance.Filter(("LiabilityType", LiabilityTypes.LIC)) + 
        GetScope<WrittenAndAccruals>(Identity).Overdue.Filter(("LiabilityType", LiabilityTypes.LIC));
    private IDataCube<ReportVariable> licData => licActuarial + accrual;
    
    private IDataCube<ReportVariable> bop => licData.Filter(("VariableType", AocTypes.BOP), ("Novelty", Novelties.I));
    private IDataCube<ReportVariable> delta => (licData.Filter(("VariableType","!BOP"),("VariableType","!EOP")) + licData.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I")))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(x => Math.Abs(x.Value) >= Precision, x => x with { Novelty = Novelties.C, VariableType = "D" });
    private IDataCube<ReportVariable> eop => licData.Filter(("VariableType",AocTypes.EOP));
    
    IDataCube<ReportVariable> Lic => bop + delta + eop;
}


public interface LrcActuarial: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<LrcActuarial>(s => s.WithApplicability<LrcActuarialPaa>(x => x.Identity.Id.ValuationApproach == ValuationApproaches.PAA));

    private IDataCube<ReportVariable> Fcf => GetScope<CurrentFcf>(Identity).CurrentFcf.Filter(("LiabilityType", LiabilityTypes.LRC));
    private IDataCube<ReportVariable> Csm => GetScope<Csm>(Identity).Csm;
    protected IDataCube<ReportVariable> Loreco => GetScope<Loreco>(Identity).Loreco;
    
    IDataCube<ReportVariable> LrcActuarial => Fcf + Csm + Loreco;
}

public interface LrcActuarialPaa: LrcActuarial{
    IDataCube<ReportVariable> LrcActuarial.LrcActuarial => -1d * GetScope<Revenues>(Identity).Revenues + -1d * GetScope<Deferrals>(Identity).Deferrals + Loreco
        + GetScope<BestEstimate>(Identity).BestEstimate
            .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.PR) ||
                GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.DE))
            .ToDataCube();
}


public interface Lrc: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    protected IDataCube<ReportVariable> lrcActuarial => GetScope<LrcActuarial>(Identity).LrcActuarial;
    protected IDataCube<ReportVariable> accrual => GetScope<WrittenAndAccruals>(Identity).Advance.Filter(("LiabilityType", LiabilityTypes.LRC)) + 
                                                 GetScope<WrittenAndAccruals>(Identity).Overdue.Filter(("LiabilityType", LiabilityTypes.LRC));
    protected IDataCube<ReportVariable> lrcData => lrcActuarial + accrual;

    private IDataCube<ReportVariable> bop => lrcData.Filter(("VariableType", AocTypes.BOP), ("Novelty", Novelties.I));
    private IDataCube<ReportVariable> delta => (lrcData.Filter(("VariableType","!BOP"),("VariableType","!EOP")) + lrcData.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I")))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(x => Math.Abs(x.Value) >= Precision, x => x with { Novelty = Novelties.C, VariableType = "D" });
    private IDataCube<ReportVariable> eop => lrcData.Filter(("VariableType",AocTypes.EOP));
    
    IDataCube<ReportVariable> Lrc => bop + delta + eop;
}


public interface FcfChangeInEstimate: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    private IDataCube<ReportVariable> FcfDeltas => GetScope<Fcf>(Identity).Fcf.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                                   GetScope<Fcf>(Identity).Fcf.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"))
                                                   .Where(x => string.IsNullOrWhiteSpace(x.AmountType) ? true : !GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.DE))
                                                   .ToDataCube();
    
    private IDataCube<ReportVariable> CurrentFcfDeltas => GetScope<CurrentFcf>(Identity).CurrentFcf.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                                          GetScope<CurrentFcf>(Identity).CurrentFcf.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"))
                                                          .Where(x => string.IsNullOrWhiteSpace(x.AmountType) ? true : !GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.DE))
                                                          .ToDataCube();

    // Non-Financial Fp
    private string variableTypeNonFinancial => Identity.Id switch {
            { LiabilityType: LiabilityTypes.LRC, IsReinsurance: false } => "IR5",
            { LiabilityType: LiabilityTypes.LRC, IsReinsurance: true } => "ISE10",
            { LiabilityType: LiabilityTypes.LIC } => "ISE12"
            };
    
    private IDataCube<ReportVariable> NonFinancialFcfDeltas => FcfDeltas
        .Filter(("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"));   
    
    IDataCube<ReportVariable> FpNonFinancial => -1 * NonFinancialFcfDeltas
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = variableTypeNonFinancial });    
    
    // Financial Fp
    private string variableTypeFpFinancial => Identity.Id switch {
            { LiabilityType: LiabilityTypes.LRC } => "IFIE1",
            { LiabilityType: LiabilityTypes.LIC } => "IFIE2",
            };
    
    // OCI 
    private string variableTypeOciFinancial => Identity.Id switch {
            { LiabilityType: LiabilityTypes.LRC } => "OCI1",
            { LiabilityType: LiabilityTypes.LIC } => "OCI2",
            };
    
    private IDataCube<ReportVariable> FinancialFcfDeltas => FcfDeltas.Filter(("VariableType", AocTypes.IA)) + 
                                                            FcfDeltas.Filter(("VariableType", AocTypes.YCU)) +
                                                            FcfDeltas.Filter(("VariableType", AocTypes.CRU));
    
    IDataCube<ReportVariable> FpFx => -1 * FcfDeltas
        .Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "IFIE3"});
    
    IDataCube<ReportVariable> FpFinancial => -1 * FinancialFcfDeltas
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = variableTypeFpFinancial});

    IDataCube<ReportVariable> OciFx => (FcfDeltas - CurrentFcfDeltas)
        .Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "OCI3"});

    IDataCube<ReportVariable> OciFinancial => (FcfDeltas - CurrentFcfDeltas)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = variableTypeOciFinancial});
}


public interface CsmChangeInEstimate: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    
    private (string amortization, string nonFinancial) variableType => Identity.Id switch {
            { IsReinsurance: false} => ("IR3", "IR5"),
            { IsReinsurance: true } => ("ISE7", "ISE10")
            };
    
    private IDataCube<ReportVariable> Csm => GetScope<Csm>(Identity).Csm.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                             GetScope<Csm>(Identity).Csm.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));
    
    IDataCube<ReportVariable> Amortization => -1 * Csm.Filter(("VariableType", AocTypes.AM)).SelectToDataCube(v => v with { VariableType = variableType.amortization });
    
    IDataCube<ReportVariable> NonFinancialChanges => -1 * Csm
        .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = variableType.nonFinancial });

    IDataCube<ReportVariable> Fx => -1 * Csm.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE3" });

    IDataCube<ReportVariable> FinancialChanges => -1 * (Csm.Filter(("VariableType", AocTypes.IA)) +
                                                        Csm.Filter(("VariableType", AocTypes.YCU)) +
                                                        Csm.Filter(("VariableType", AocTypes.CRU)) )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE1" });
}


public interface LcChangeInEstimate: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    
    private IDataCube<ReportVariable> Lc => GetScope<Lc>(Identity).Lc.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                            GetScope<Lc>(Identity).Lc.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));
    
    IDataCube<ReportVariable> Amortization => -1 * Lc.Filter(("VariableType", AocTypes.AM)).SelectToDataCube(v => v with { VariableType = "ISE9" });
    
    IDataCube<ReportVariable> NonFinancialChanges => -1 * Lc
        .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

    IDataCube<ReportVariable> NonFinancialChangesToIr => -1 * (Amortization + NonFinancialChanges).SelectToDataCube(v => v with { VariableType = "IR5" });
    
    IDataCube<ReportVariable> Fx => -1 * Lc.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { VariableType = "IFIE3" });

    IDataCube<ReportVariable> FinancialChanges =>  1 * (Lc.Filter(("VariableType", AocTypes.IA)) +
                                                        Lc.Filter(("VariableType", AocTypes.YCU)) +
                                                        Lc.Filter(("VariableType", AocTypes.CRU)) )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE1" });

    IDataCube<ReportVariable> FinancialChangesToIse => -1 * FinancialChanges.SelectToDataCube(v => v with { VariableType = "ISE11" });
}


public interface LorecoChangeInEstimate: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    
    private IDataCube<ReportVariable> Loreco => GetScope<Loreco>(Identity).Loreco.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                                GetScope<Loreco>(Identity).Loreco.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));
    
    IDataCube<ReportVariable> Amortization => -1 * Loreco.Filter(("VariableType", AocTypes.AM)).SelectToDataCube(v => v with { VariableType = "ISE8" });
    
    IDataCube<ReportVariable> NonFinancialChanges => -1 * Loreco
        .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

    IDataCube<ReportVariable> Fx => -1 * Loreco.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { VariableType = "IFIE3" });

    IDataCube<ReportVariable> FinancialChangesToIse =>  -1 * (Loreco.Filter(("VariableType", AocTypes.IA)) +
                                                        Loreco.Filter(("VariableType", AocTypes.YCU)) +
                                                        Loreco.Filter(("VariableType", AocTypes.CRU)) )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });
}


public interface IncurredActuals: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    private IDataCube<ReportVariable> WrittenCashflow => GetScope<WrittenAndAccruals>(Identity).Written.Filter(("VariableType", "CF"));
    private IDataCube<ReportVariable> AdvanceWriteOff => GetScope<WrittenAndAccruals>(Identity).Advance.Filter(("VariableType", "WO"));
    private IDataCube<ReportVariable> OverdueWriteOff => GetScope<WrittenAndAccruals>(Identity).Overdue.Filter(("VariableType", "WO"));
    private IDataCube<ReportVariable> EffectiveActuals => WrittenCashflow -1 * (AdvanceWriteOff + OverdueWriteOff);
    
    private string premiumsVariableType => Identity.Id switch {
            { IsReinsurance: false} => "IR1",
            { IsReinsurance: true } => "ISE1"
            }; 
    
    IDataCube<ReportVariable> Premiums => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.PR))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = premiumsVariableType });
    
    IDataCube<ReportVariable> Claims => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.CL))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE2" });
    
    private IDataCube<ReportVariable> ClaimsIco => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.ICO)).ToDataCube();
    
    IDataCube<ReportVariable> ClaimsIcoToIr => ClaimsIco.SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR2" }); //TODO, add Reinsurance case
    IDataCube<ReportVariable> ClaimsIcoToIse => (-1 * ClaimsIco).SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE5" });
    
    IDataCube<ReportVariable> Expenses => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.AE))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE3" });

    IDataCube<ReportVariable> Commissions => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.AC))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE4" });

    IDataCube<ReportVariable> ClaimExpenses => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.CE))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE41" });
}


public interface IncurredDeferrals: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    private IDataCube<ReportVariable> Deferrals => GetScope<Deferrals>(Identity).Filter(("VariableType", "!BOP"),("VariableType", "!EOP"));
        
    private IDataCube<ReportVariable> Amortization => -1 * Deferrals
        .Filter(("VariableType", AocTypes.AM));
    
    IDataCube<ReportVariable> AmortizationToIr => (-1 * Amortization).SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR4" });
    IDataCube<ReportVariable> AmortizationToIse => Amortization.SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE6" });
}


public interface ExperienceAdjustmentOnPremium: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<ExperienceAdjustmentOnPremium>(s => s.WithApplicability<ExperienceAdjustmentOnPremiumNotApplicable>(x => x.Identity.Id.IsReinsurance || x.Identity.Id.LiabilityType == LiabilityTypes.LIC));

    private IDataCube<ReportVariable> WrittenPremium => GetScope<WrittenAndAccruals>(Identity).Written.Filter(("VariableType", "CF"))
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.PR)).ToDataCube();
    private IDataCube<ReportVariable> BestEstimatePremium => GetScope<BestEstimate>(Identity).BestEstimate.Filter(("VariableType", "CF"))
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.PR)).ToDataCube();
    private IDataCube<ReportVariable> WrittenPremiumToCsm => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.APA)).FxData;
    private IDataCube<ReportVariable> BestEstimatePremiumToCsm => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.BEPA)).FxData;

    IDataCube<ReportVariable> ExperienceAdjustmentOnPremiumTotal => -1 * (WrittenPremium - BestEstimatePremium)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR77" });

    IDataCube<ReportVariable> ExperienceAdjustmentOnPremiumToCsm => (WrittenPremiumToCsm.SelectToDataCube(v => v with { EstimateType = EstimateTypes.BE}) - BestEstimatePremiumToCsm.SelectToDataCube(v => v with { EstimateType = EstimateTypes.A}))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR78" });

    IDataCube<ReportVariable> ExperienceAdjustmentOnPremiumToRev => ((WrittenPremium - WrittenPremiumToCsm).AggregateOver(nameof(EstimateType)).SelectToDataCube(v => v with { EstimateType = EstimateTypes.A })
         - (BestEstimatePremium - BestEstimatePremiumToCsm).AggregateOver(nameof(EstimateType)).SelectToDataCube(v => v with { EstimateType = EstimateTypes.BE }))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR79" });
}

public interface ExperienceAdjustmentOnPremiumNotApplicable: ExperienceAdjustmentOnPremium {
    IDataCube<ReportVariable> ExperienceAdjustmentOnPremium.ExperienceAdjustmentOnPremiumTotal => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube();
    IDataCube<ReportVariable> ExperienceAdjustmentOnPremium.ExperienceAdjustmentOnPremiumToCsm => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube();
    IDataCube<ReportVariable> ExperienceAdjustmentOnPremium.ExperienceAdjustmentOnPremiumToRev => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube();    
}


// public interface ExperienceAdjustmentOnAcquistionExpenses: IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
//     static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
//         builder.ForScope<ExperienceAdjustmentOnAcquistionExpenses>(s => s.WithApplicability<ExperienceAdjustmentOnAcquistionExpensesNotApplicable>(x => x.Identity.Id.IsReinsurance || x.Identity.Id.LiabilityType == LiabilityTypes.LIC));

//     private IDataCube<ReportVariable> WrittenAcquistionExpenses => GetScope<WrittenAndAccruals>(Identity).Written.Filter(("VariableType", "CF"))
//         .Where(x =>
//             GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.AEA) ||
//             GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.ACA)).ToDataCube();
//     private IDataCube<ReportVariable> BestEstimateAcquistionExpenses => GetScope<BestEstimate>(Identity).BestEstimate.Filter(("VariableType", "CF"))
//         .Where(x =>
//             GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.AEA) ||
//             GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.ACA)).ToDataCube();

//     IDataCube<ReportVariable> ExperienceAdjustmentOnAcquistionExpenses => (WrittenAcquistionExpenses - BestEstimateAcquistionExpenses)
//         .AggregateOver(nameof(Novelty), nameof(VariableType))
//         .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR8" });

// }

// public interface ExperienceAdjustmentOnAcquistionExpensesNotApplicable: ExperienceAdjustmentOnAcquistionExpenses {
//     IDataCube<ReportVariable> ExperienceAdjustmentOnAcquistionExpenses.ExperienceAdjustmentOnAcquistionExpenses=> Enumerable.Empty<ReportVariable>().ToArray().ToDataCube();      
// }


public interface FinancialPerformance: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    
    private IDataCube<ReportVariable> FcfChangeInEstimate => GetScope<FcfChangeInEstimate>(Identity);
    private IDataCube<ReportVariable> CsmChangeInEstimate => GetScope<CsmChangeInEstimate>(Identity);
    private IDataCube<ReportVariable> LcChangeInEstimate => GetScope<LcChangeInEstimate>(Identity);
    private IDataCube<ReportVariable> LorecoChangeInEstimate => GetScope<LorecoChangeInEstimate>(Identity);
    private IDataCube<ReportVariable> IncurredActuals => GetScope<IncurredActuals>(Identity);
    private IDataCube<ReportVariable> IncurredDeferrals => GetScope<IncurredDeferrals>(Identity);
    private IDataCube<ReportVariable> ExperienceAdjustmentOnPremium => GetScope<ExperienceAdjustmentOnPremium>(Identity);
    //private IDataCube<ReportVariable> ExperienceAdjustmentOnAcquistionExpenses => GetScope<ExperienceAdjustmentOnAcquistionExpenses>(Identity);
    
    IDataCube<ReportVariable> FinancialPerformance => FcfChangeInEstimate + CsmChangeInEstimate + LcChangeInEstimate + LorecoChangeInEstimate + IncurredActuals + IncurredDeferrals + ExperienceAdjustmentOnPremium;
}


public interface InsuranceRevenue: IScope<(ReportIdentity Id, OpenSmc.Ifrs17.Domain.Constants.CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<InsuranceRevenue>(s => s.WithApplicability<InsuranceRevenueNotApplicable>(x => x.Identity.Id.IsReinsurance || x.Identity.Id.LiabilityType == LiabilityTypes.LIC));

    // PAA Premiums
    private IDataCube<ReportVariable> WrittenCashflow => GetScope<WrittenAndAccruals>(Identity).Written.Filter(("VariableType", "CF"));
    private IDataCube<ReportVariable> AdvanceWriteOff => GetScope<WrittenAndAccruals>(Identity).Advance.Filter(("VariableType", "WO"));
    private IDataCube<ReportVariable> OverdueWriteOff => GetScope<WrittenAndAccruals>(Identity).Overdue.Filter(("VariableType", "WO"));
    private IDataCube<ReportVariable> EffectiveActuals => WrittenCashflow -1 * (AdvanceWriteOff + OverdueWriteOff);
    private IDataCube<ReportVariable> Revenues => GetScope<Revenues>(Identity).Revenues.Filter(("VariableType", "AM"));
    
    private IDataCube<ReportVariable> PaaPremiums => Identity.Id switch {
            { ValuationApproach: ValuationApproaches.PAA } => Revenues
                .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR11" }),
            _ => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube()
    };

    // Experience Adjustment On Premiums
    private IDataCube<ReportVariable> NotPaaActualPremiums => Identity.Id switch {
        { ValuationApproach: ValuationApproaches.PAA } => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(),
        _ => EffectiveActuals
            .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.PR))
            .SelectToDataCube(v => v with { Novelty = Novelties.C }) 
    };

    private IDataCube<ReportVariable> NotPaaBestEstimatePremiums => Identity.Id switch {
        { ValuationApproach: ValuationApproaches.PAA } => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(),
        _ => GetScope<BestEstimate>(Identity).BestEstimate
            .Filter(("VariableType", "CF"), ("LiabilityType", "LRC"))
            .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.PR))
            .AggregateOver(nameof(Novelty))
            .SelectToDataCube(rv => rv with { Novelty = Novelties.C })
    };

    private IDataCube<ReportVariable> WrittenPremiumsToCsm => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.APA)).FxData;
    private IDataCube<ReportVariable> BestEstimatePremiumsToCsm => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.BEPA)).FxData;

    private IDataCube<ReportVariable> ExperienceAdjustmentOnPremiums => (
            (NotPaaActualPremiums - WrittenPremiumsToCsm).AggregateOver(nameof(EstimateType)).SelectToDataCube(rv => rv with { EstimateType = EstimateTypes.A }) -
            (NotPaaBestEstimatePremiums - BestEstimatePremiumsToCsm).AggregateOver(nameof(EstimateType)).SelectToDataCube(rv => rv with { EstimateType = EstimateTypes.BE })
        )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR12" });        

    // Expected Best Estimate cash flow out Release
    private IDataCube<ReportVariable> CfOut => -1 * GetScope<BestEstimate>(Identity).BestEstimate
        .Filter(("VariableType", "CF"), ("LiabilityType", "LRC"))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C });
        
    private IDataCube<ReportVariable> ExpectedClaims => CfOut // --> Exclude NA Expenses
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.CL))
        .SelectToDataCube(v => v with { VariableType = "IR13" });
        
    private IDataCube<ReportVariable> ExpectedClaimsInvestmentComponent => -1 * CfOut
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.ICO))
        .SelectToDataCube(v => v with { VariableType = "IR2" });

    private IDataCube<ReportVariable> ExpectedExpenses => CfOut
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.AE))
        .SelectToDataCube(v => v with { VariableType = "IR13" });

    private IDataCube<ReportVariable> ExpectedCommissions => CfOut
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.AC))
        .SelectToDataCube(v => v with { VariableType = "IR13" });

    // RA Release
    private IDataCube<ReportVariable> RaRelease => -1 * GetScope<RiskAdjustment>(Identity).RiskAdjustment
        .Filter(("VariableType", "CF"), ("LiabilityType", "LRC"))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "IR13" });

    // CSM Release (Amortization)
    private IDataCube<ReportVariable> CsmAmortization => -1 * GetScope<Csm>(Identity).Csm
        .Filter(("VariableType", AocTypes.AM))
        .SelectToDataCube(v => v with { VariableType = "IR13" });
   
    // Loss Component Release (Amortization)
    private IDataCube<ReportVariable> LossComponentAmortization => GetScope<Lc>(Identity).Lc
        .Filter(("VariableType", AocTypes.AM))
        .SelectToDataCube(v => v with { VariableType = "IR13" });

    // Acquistion Expenses Release (Amortization)
    private IDataCube<ReportVariable> AcquistionExpensesAmortization => Identity.Id switch {
        { ValuationApproach: ValuationApproaches.PAA } => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(),
        _ => -1 * GetScope<Deferrals>(Identity)
            .Filter(("VariableType", AocTypes.AM))
            .SelectToDataCube(v => v with { VariableType = "IR13" })};

    // Financial on Deferrals Correction
    private IDataCube<ReportVariable> FinancialOnDeferrals => -1 * GetScope<Deferrals>(Identity).Deferrals
        .Filter(x => x.VariableType == AocTypes.IA || x.VariableType == AocTypes.YCU || x.VariableType == AocTypes.CRU || x.VariableType == AocTypes.FX)
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR9" });

    // FCF Locked-In Interest Rate Correction
    private IDataCube<ReportVariable> FcfDeltas => GetScope<Fcf>(Identity).Fcf.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                                   GetScope<Fcf>(Identity).Fcf.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));
    
    private IDataCube<ReportVariable> LockedFcfDeltas => GetScope<LockedFcf>(Identity).LockedFcf.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                                          GetScope<LockedFcf>(Identity).LockedFcf.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));

    private IDataCube<ReportVariable> NonFinancialFcfDeltas => FcfDeltas
        .Filter(("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"));
    
    private IDataCube<ReportVariable> NonFinancialLockedFcfDeltas => LockedFcfDeltas
        .Filter(("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"));

    private IDataCube<ReportVariable> NonFinancialFcfDeltasCorrection => -1 * (NonFinancialFcfDeltas - NonFinancialLockedFcfDeltas)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "IR14"});

    // InsuranceRevenue   
    IDataCube<ReportVariable> InsuranceRevenue => PaaPremiums + ExperienceAdjustmentOnPremiums + RaRelease + CsmAmortization + LossComponentAmortization + ExpectedClaims + ExpectedClaimsInvestmentComponent + ExpectedExpenses + ExpectedCommissions + AcquistionExpensesAmortization + FinancialOnDeferrals + NonFinancialFcfDeltasCorrection;

}

public interface InsuranceRevenueNotApplicable : InsuranceRevenue {
    IDataCube<ReportVariable> InsuranceRevenue.InsuranceRevenue => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(); 
}


public interface InsuranceServiceExpense: IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<InsuranceServiceExpense>(s => s.WithApplicability<InsuranceServiceExpenseReinsurance>(x => x.Identity.Id.IsReinsurance));

    // Actuals cash flow out Release
    private IDataCube<ReportVariable> WrittenCashflow => GetScope<WrittenAndAccruals>(Identity).Written.Filter(("VariableType", "CF"));
    private IDataCube<ReportVariable> AdvanceWriteOff => GetScope<WrittenAndAccruals>(Identity).Advance.Filter(("VariableType", "WO"));
    private IDataCube<ReportVariable> OverdueWriteOff => GetScope<WrittenAndAccruals>(Identity).Overdue.Filter(("VariableType", "WO"));
    protected IDataCube<ReportVariable> EffectiveActuals => WrittenCashflow -1 * (AdvanceWriteOff + OverdueWriteOff);   
    
    private IDataCube<ReportVariable> ActualClaims => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.CL))
        .SelectToDataCube(v => v with { VariableType = "ISE2" });
    private IDataCube<ReportVariable> ActualClaimsInvestmentComponent => -1 * EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.ICO))
        .SelectToDataCube(v => v with { VariableType = "ISE5" });

    private IDataCube<ReportVariable> ActualExpenses => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.AE))
        .SelectToDataCube(v => v with { VariableType = "ISE3" });

    private IDataCube<ReportVariable> ActualCommissions => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.AC))
        .SelectToDataCube(v => v with { VariableType = "ISE4" });

    private IDataCube<ReportVariable> ActualClaimExpenses => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.CE))
        .SelectToDataCube(v => v with { VariableType = "ISE41" });

    // Acquistion Expenses Release (Amortization)
    private IDataCube<ReportVariable> AcquistionExpensesAmortization => GetScope<Deferrals>(Identity)
        .Filter(("VariableType", AocTypes.AM))
        .SelectToDataCube(v => v with { VariableType = "ISE6" });

    // Financial on Deferrals Correction
    private IDataCube<ReportVariable> FinancialOnDeferrals => GetScope<Deferrals>(Identity).Deferrals
        .Filter(x => x.VariableType == AocTypes.IA || x.VariableType == AocTypes.YCU || x.VariableType == AocTypes.CRU || x.VariableType == AocTypes.FX)
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE13" }); 

    // Loss Component
    private IDataCube<ReportVariable> Lc => GetScope<Lc>(Identity).Lc.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                            GetScope<Lc>(Identity).Lc.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));
    
    private IDataCube<ReportVariable> LcAmortization => -1 * Lc.Filter(("VariableType", AocTypes.AM)).SelectToDataCube(v => v with { VariableType = "ISE9" });
    
    private IDataCube<ReportVariable> LcNonFinancialChanges => -1 * Lc
        .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

    private IDataCube<ReportVariable> LcFinancialChanges => -1 * (Lc.Filter(("VariableType", AocTypes.IA)) +
                                                        Lc.Filter(("VariableType", AocTypes.YCU)) +
                                                        Lc.Filter(("VariableType", AocTypes.CRU)) )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

    // Change in LIC
    private IDataCube<ReportVariable> FcfDeltas => (GetScope<Fcf>(Identity).Fcf.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                                   GetScope<Fcf>(Identity).Fcf.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I")))
                                                   .Filter(("LiabilityType", "LIC"));  // TODO, extract the LIC to a dedicated scope (whole thing, actually)
    
    private IDataCube<ReportVariable> NonFinancialFcfDeltas => FcfDeltas
        .Filter(("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"));

    private IDataCube<ReportVariable> FpNonFinancialLic => -1 * NonFinancialFcfDeltas
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "ISE12" });

    // Reinsurance
    protected IDataCube<ReportVariable> Reinsurance => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(); 

    // Insurance Service Expense   
    IDataCube<ReportVariable> InsuranceServiceExpense => ActualClaims + ActualClaimsInvestmentComponent + ActualExpenses + ActualCommissions + ActualClaimExpenses + AcquistionExpensesAmortization + FinancialOnDeferrals + LcAmortization + LcNonFinancialChanges + LcFinancialChanges + FpNonFinancialLic + Reinsurance;
}

public interface InsuranceServiceExpenseReinsurance : InsuranceServiceExpense {
    // Expected Best Estimate cash flow out Release
    private IDataCube<ReportVariable> CfOut => -1 * GetScope<BestEstimate>(Identity).BestEstimate
        .Filter(("VariableType", "CF"), ("LiabilityType", "LRC"))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C });
        
    private IDataCube<ReportVariable> ExpectedClaims => CfOut // --> Exclude NA Expenses
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.CL))
        .SelectToDataCube(v => v with { VariableType = "ISE22" });
    private IDataCube<ReportVariable> ExpectedClaimsInvestmentComponent => -1 * CfOut
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.ICO))
        .SelectToDataCube(v => v with { VariableType = "ISE23" });

    private IDataCube<ReportVariable> ExpectedExpenses => CfOut
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.AE))
        .SelectToDataCube(v => v with { VariableType = "ISE22" });

    private IDataCube<ReportVariable> ExpectedCommissions => CfOut
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.AC))
        .SelectToDataCube(v => v with { VariableType = "ISE22" });

    // RA Release
    private IDataCube<ReportVariable> RaRelease => -1 * GetScope<RiskAdjustment>(Identity).RiskAdjustment
        .Filter(("VariableType", "CF"), ("LiabilityType", "LRC"))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "ISE22" });

    // CSM Release (Amortization)
    private IDataCube<ReportVariable> CsmAmortization => -1 * GetScope<Csm>(Identity).Csm
        .Filter(("VariableType", AocTypes.AM))
        .SelectToDataCube(v => v with { VariableType = "ISE22" });

    // Acquistion Expenses Release (Amortization)
    private IDataCube<ReportVariable> AcquistionExpensesAmortization => Identity.Id switch {
        { ValuationApproach: ValuationApproaches.PAA } => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(),
        _ => -1 * GetScope<Deferrals>(Identity)
            .Filter(("VariableType", AocTypes.AM))
            .SelectToDataCube(v => v with { VariableType = "ISE22" })
    };  

    // Financial on Deferrals Correction
    private IDataCube<ReportVariable> FinancialOnDeferralsToRiRevenue => -1d * GetScope<Deferrals>(Identity).Deferrals
        .Filter(x => x.VariableType == AocTypes.IA || x.VariableType == AocTypes.YCU || x.VariableType == AocTypes.CRU || x.VariableType == AocTypes.FX)
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE25" }); 

    // Loss Recovery Component (Amortization)
    private IDataCube<ReportVariable> Loreco => GetScope<Loreco>(Identity).Loreco.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                                GetScope<Loreco>(Identity).Loreco.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));

    private IDataCube<ReportVariable> LorecoAmortization => -1 * Loreco
        .Filter(("VariableType", AocTypes.AM))
        .SelectToDataCube(v => v with { VariableType = "ISE8" });
        
    private IDataCube<ReportVariable> LorecoNonFinancialChanges => -1 * Loreco
        .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

    private IDataCube<ReportVariable> LorecoFinancialChanges => -1 * (Loreco.Filter(("VariableType", AocTypes.IA)) +
                                                        Loreco.Filter(("VariableType", AocTypes.YCU)) +
                                                        Loreco.Filter(("VariableType", AocTypes.CRU)) )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

    // PAA Premiums
    private IDataCube<ReportVariable> Revenues => GetScope<Revenues>(Identity).Revenues.Filter(("VariableType", "AM"));

    private IDataCube<ReportVariable> PaaPremiums => Identity.Id switch {
            { ValuationApproach: ValuationApproaches.PAA } => Revenues
                .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE201" }),
            _ => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube()
    };

    // Experience Adjustment On Premiums
    private IDataCube<ReportVariable> ReinsuranceActualPremiums => Identity.Id switch {
        { ValuationApproach: ValuationApproaches.PAA } => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(),
        _ => EffectiveActuals
            .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.PR))
            .SelectToDataCube(v => v with { Novelty = Novelties.C }) 
    };

    private IDataCube<ReportVariable> ReinsuranceBestEstimatePremiums => Identity.Id switch {
        { ValuationApproach: ValuationApproaches.PAA } => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(),
        _ => GetScope<BestEstimate>(Identity).BestEstimate
            .Filter(("VariableType", "CF"), ("LiabilityType", "LRC"))
            .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.PR))
            .AggregateOver(nameof(Novelty))
            .SelectToDataCube(rv => rv with { Novelty = Novelties.C })
    };

    private IDataCube<ReportVariable> ExperienceAdjustmentOnPremiums => (
            (ReinsuranceActualPremiums).AggregateOver(nameof(EstimateType)).SelectToDataCube(rv => rv with { EstimateType = EstimateTypes.A }) -
            (ReinsuranceBestEstimatePremiums).AggregateOver(nameof(EstimateType)).SelectToDataCube(rv => rv with { EstimateType = EstimateTypes.BE })
        )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE21" });

    // FCF Locked-In Interest Rate Correction
    private IDataCube<ReportVariable> FcfDeltas => GetScope<Fcf>(Identity).Fcf.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                                   GetScope<Fcf>(Identity).Fcf.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));
    
    private IDataCube<ReportVariable> LockedFcfDeltas => GetScope<LockedFcf>(Identity).LockedFcf.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                                          GetScope<LockedFcf>(Identity).LockedFcf.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));

    private IDataCube<ReportVariable> NonFinancialFcfDeltas => FcfDeltas
        .Filter(("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"));
    
    private IDataCube<ReportVariable> NonFinancialLockedFcfDeltas => LockedFcfDeltas
        .Filter(("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"));

    private IDataCube<ReportVariable> NonFinancialFcfDeltasCorrection => (Identity.Id.LiabilityType == LiabilityTypes.LIC)
        ? Enumerable.Empty<ReportVariable>().ToArray().ToDataCube()
        : -1 * (NonFinancialFcfDeltas - NonFinancialLockedFcfDeltas)
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "ISE24"});

    // Reinsurance
    IDataCube<ReportVariable> InsuranceServiceExpense.Reinsurance => ExpectedClaims + ExpectedClaimsInvestmentComponent + ExpectedExpenses + ExpectedCommissions + RaRelease + CsmAmortization + AcquistionExpensesAmortization + FinancialOnDeferralsToRiRevenue + LorecoAmortization + LorecoNonFinancialChanges + LorecoFinancialChanges + PaaPremiums + ExperienceAdjustmentOnPremiums + NonFinancialFcfDeltasCorrection;
}



public interface InsuranceFinanceIncomeExpenseOci: IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    //FCF
    private IDataCube<ReportVariable> FcfDeltas => GetScope<Fcf>(Identity).Fcf.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
            GetScope<Fcf>(Identity).Fcf.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));
    
    private IDataCube<ReportVariable> CurrentFcfDeltas => Identity.Id switch {
        { ValuationApproach: ValuationApproaches.PAA, LiabilityType: LiabilityTypes.LRC } => FcfDeltas,
        _ => GetScope<CurrentFcf>(Identity).CurrentFcf.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
             GetScope<CurrentFcf>(Identity).CurrentFcf.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"))
    };
  
    // Financial Fp
    private string variableTypeFpFinancial => Identity.Id switch {
            { LiabilityType: LiabilityTypes.LRC } => "IFIE1",
            { LiabilityType: LiabilityTypes.LIC } => "IFIE2",
            };
    
    // OCI 
    private string variableTypeOciFinancial => Identity.Id switch {
            { LiabilityType: LiabilityTypes.LRC } => "OCI1",
            { LiabilityType: LiabilityTypes.LIC } => "OCI2",
            };
    
    private IDataCube<ReportVariable> FinancialFcfDeltas => FcfDeltas.Filter(("VariableType", AocTypes.IA)) + 
                                                            FcfDeltas.Filter(("VariableType", AocTypes.YCU)) +
                                                            FcfDeltas.Filter(("VariableType", AocTypes.CRU));
    
    private IDataCube<ReportVariable> FpFcfFx => -1 * FcfDeltas
        .Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "IFIE3"});
    
    private IDataCube<ReportVariable> FpFcfFinancial => -1 * FinancialFcfDeltas
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = variableTypeFpFinancial});

    private IDataCube<ReportVariable> OciFcfFx => (FcfDeltas - CurrentFcfDeltas)
        .Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "OCI3"});

    private IDataCube<ReportVariable> OciFcfFinancial => (FcfDeltas - CurrentFcfDeltas)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = variableTypeOciFinancial});

    // CSM
    private IDataCube<ReportVariable> Csm => GetScope<Csm>(Identity).Csm.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                             GetScope<Csm>(Identity).Csm.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));

    private IDataCube<ReportVariable> CsmFx => -1 * Csm.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE3" });

    private IDataCube<ReportVariable> CsmFinancialChanges => -1 * (Csm.Filter(("VariableType", AocTypes.IA)) +
                                                        Csm.Filter(("VariableType", AocTypes.YCU)) +
                                                        Csm.Filter(("VariableType", AocTypes.CRU)) )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE1" });

    // LC
    private IDataCube<ReportVariable> Lc => GetScope<Lc>(Identity).Lc.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                            GetScope<Lc>(Identity).Lc.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));

    private IDataCube<ReportVariable> LcFx => -1 * Lc.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { VariableType = "IFIE3" });

    private IDataCube<ReportVariable> LcFinancialChanges => 1 * (Lc.Filter(("VariableType", AocTypes.IA)) +
                                                        Lc.Filter(("VariableType", AocTypes.YCU)) +
                                                        Lc.Filter(("VariableType", AocTypes.CRU)) )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE1" });

    // LoReCo
    private IDataCube<ReportVariable> Loreco => GetScope<Loreco>(Identity).Loreco.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                                GetScope<Loreco>(Identity).Loreco.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));

    private IDataCube<ReportVariable> LorecoFx => -1 * Loreco.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { VariableType = "IFIE3" });

    // PAA Revenues
   private IDataCube<ReportVariable> PaaRevenue => GetScope<Revenues>(Identity).Revenues.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
                                                   GetScope<Revenues>(Identity).Revenues.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I"));

    private IDataCube<ReportVariable> PaaRevenueFx => -1 * PaaRevenue.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { VariableType = "IFIE3" });

    private IDataCube<ReportVariable> PaaRevenueFinancialChanges => 1 * (PaaRevenue.Filter(("VariableType", AocTypes.IA)) +
                                                                PaaRevenue.Filter(("VariableType", AocTypes.YCU)) +
                                                                PaaRevenue.Filter(("VariableType", AocTypes.CRU)) )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = variableTypeFpFinancial });

    // PAA Deferrals
    private IDataCube<ReportVariable> PaaDeferrals => Identity.Id switch {
        { ValuationApproach: ValuationApproaches.PAA, LiabilityType: LiabilityTypes.LRC }
            => GetScope<Deferrals>(Identity).Deferrals.Filter(("VariableType", "!BOP"),("VariableType", "!EOP")) +
               GetScope<Deferrals>(Identity).Deferrals.Filter(("VariableType", AocTypes.BOP),("Novelty", "!I")),
        _ => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube()
        };

    private IDataCube<ReportVariable> PaaDeferralsFx => -1 * PaaDeferrals.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { VariableType = "IFIE3" });

    private IDataCube<ReportVariable> PaaDeferralsFinancialChanges => 1 * (PaaDeferrals.Filter(("VariableType", AocTypes.IA)) +
                                                                PaaDeferrals.Filter(("VariableType", AocTypes.YCU)) +
                                                                PaaDeferrals.Filter(("VariableType", AocTypes.CRU)) )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = variableTypeFpFinancial });

    //Insurance Finance Income/Expense Oci
    IDataCube<ReportVariable> InsuranceFinanceIncomeExpenseOci => FpFcfFx + FpFcfFinancial + OciFcfFx + OciFcfFinancial + CsmFx + CsmFinancialChanges + LcFx + LcFinancialChanges + LorecoFx
     + PaaRevenueFinancialChanges + PaaRevenueFx + PaaDeferralsFinancialChanges + PaaDeferralsFx;
}


public interface FinancialPerformanceAlternative: IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
    
    private IDataCube<ReportVariable> InsuranceRevenue => GetScope<InsuranceRevenue>(Identity);
    private IDataCube<ReportVariable> InsuranceServiceExpense => GetScope<InsuranceServiceExpense>(Identity);
    private IDataCube<ReportVariable> InsuranceFinanceIncomeExpenseOci => GetScope<InsuranceFinanceIncomeExpenseOci>(Identity);
    
    IDataCube<ReportVariable> FinancialPerformanceAlternative => InsuranceRevenue + InsuranceServiceExpense + InsuranceFinanceIncomeExpenseOci;
}



