using OpenSmc.Scopes;
using OpenSmc.DataCubes;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.ReportHub;

public static class ReportScopes
{
    public interface IUniverse : IScopeWithStorage<ReportStorage>
    {
    }

    public interface Data : IScope<(ReportIdentity ReportIdentity, string EstimateType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
            builder.ForScope<Data>(s =>
                s.WithApplicability<DataWrittenActual>(x =>
                    x.GetStorage().EstimateTypesWithoutAoc.Contains(x.Identity.EstimateType)));

        protected IDataCube<ReportVariable> RawData =>
            GetStorage().GetVariables(Identity.ReportIdentity, Identity.EstimateType);

        private IDataCube<ReportVariable> RawEops => RawData.Filter(("VariableType", AocTypes.EOP));

        private IDataCube<ReportVariable> NotEopsNotCls =>
            RawData.Filter(("VariableType", "!EOP"),
                ("VariableType", "!CL")); // TODO negation must be hardcoded (also to avoid string concatenation)

        private IDataCube<ReportVariable> CalculatedCl => (RawEops - NotEopsNotCls)
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(x => Math.Abs(x.Value) >= Consts.Precision,
                x => x with { Novelty = Novelties.C, VariableType = AocTypes.CL });

        private IDataCube<ReportVariable> CalculatedEops => (NotEopsNotCls + CalculatedCl)
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(x =>Math.Abs(x.Value) >= Consts.Precision, 
                x => x with { VariableType = AocTypes.EOP, Novelty = Novelties.C });

        IDataCube<ReportVariable> Data => NotEopsNotCls + CalculatedCl + CalculatedEops;
    }

    public interface DataWrittenActual : Data
    {
        IDataCube<ReportVariable> Data.Data => RawData;
    }

    public interface Fx : IScope<(string ContractualCurrency, string FunctionalCurrency, FxPeriod FxPeriod, (int, int)
        Period, CurrencyType CurrencyType), ReportStorage>
    {
        private double groupFxRate => Identity.CurrencyType switch
        {
            CurrencyType.Group => GetStorage().GetFx(Identity.Period, Identity.FunctionalCurrency, GroupCurrency,
                FxPeriod.Average),
            _ => 1
        };

        private double GetFunctionalFxRate(FxPeriod fxPeriod) => Identity.CurrencyType switch
        {
            CurrencyType.Contractual => 1,
            _ => GetStorage().GetFx(Identity.Period, Identity.ContractualCurrency, Identity.FunctionalCurrency,
                fxPeriod)
        };

        double Fx => GetFunctionalFxRate(Identity.FxPeriod) * groupFxRate;
    }

    public interface FxData :
        IScope<(ReportIdentity ReportIdentity, CurrencyType CurrencyType, string EstimateType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
            builder.ForScope<FxData>(s =>
                s.WithApplicability<FxDataWrittenActual>(x =>
                    x.GetStorage().EstimateTypesWithoutAoc.Contains(x.Identity.EstimateType)));

        protected IDataCube<ReportVariable> Data => GetScope<Data>((Identity.ReportIdentity, Identity.EstimateType))
            .Data
            .SelectToDataCube(x => Multiply(GetScope<Fx>((Identity.ReportIdentity.ContractualCurrency,
                    Identity.ReportIdentity.FunctionalCurrency,
                    GetStorage().GetFxPeriod(GetStorage().Args.Period, Identity.ReportIdentity.Projection,
                        x.VariableType, x.Novelty),
                    (Identity.ReportIdentity.Year, Identity.ReportIdentity.Month),
                    Identity.CurrencyType)).Fx, x) with
                {
                    Currency = Identity.CurrencyType switch
                    {
                        CurrencyType.Contractual => x.ContractualCurrency,
                        CurrencyType.Functional => x.FunctionalCurrency,
                        _ => GroupCurrency
                    }
                });

        private IDataCube<ReportVariable> Eops => Data.Filter(("VariableType", AocTypes.EOP));

        private IDataCube<ReportVariable> NotEops =>
            Data.Filter(("VariableType",
                "!EOP")); // TODO negation must be hardcoded (also to avoid string concatenation)

        private IDataCube<ReportVariable> Fx => (Eops - NotEops)
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(x => Math.Abs(x.Value) >= Consts.Precision,
                x => x with { Novelty = Novelties.C, VariableType = AocTypes.FX });

        IDataCube<ReportVariable> FxData => Data + Fx;
    }

    public interface FxDataWrittenActual : FxData
    {
        IDataCube<ReportVariable> FxData.FxData => Data;
    }

    using System.Reflection;

    public static T[] GetAllPublicConstantValues<T>(this Type type,
        IList<T> excludedTerms = null)
    {
        var selection = type
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(T))
            .Select(x => (T)x.GetRawConstantValue())
            .ToArray();
        if (excludedTerms == null)
            return selection;
        else
            return selection.Where(x => !excludedTerms.Contains(x)).ToArray();
    }

    public interface BestEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> BestEstimate => Identity.Id switch
        {
            { ValuationApproach: ValuationApproaches.PAA, LiabilityType: LiabilityTypes.LRC } =>
                GetScope<LockedBestEstimate>(Identity)
                    .LockedBestEstimate, //TODO we should use the economic basis driver to decide which Economic basis to use
            { ValuationApproach: ValuationApproaches.BBA, IsOci: true } => GetScope<LockedBestEstimate>(Identity)
                .LockedBestEstimate,
            _ => GetScope<CurrentBestEstimate>(Identity).CurrentBestEstimate
        };
    }

    public interface LockedBestEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> LockedBestEstimate =>
            GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.BE)).FxData
                .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.L),
                    ("AmountType", "!CDRI"));
    }

    public interface CurrentBestEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> CurrentBestEstimate =>
            GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.BE)).FxData
                .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.C),
                    ("AmountType", "!CDRI"));
    }

    public interface NominalBestEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> NominalBestEstimate =>
            GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.BE)).FxData
                .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.N),
                    ("AmountType", "!CDRI"));
    }

    public interface RiskAdjustment : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> RiskAdjustment => Identity.Id switch
        {
            { ValuationApproach: ValuationApproaches.PAA, LiabilityType: LiabilityTypes.LRC } =>
                GetScope<LockedRiskAdjustment>(Identity)
                    .LockedRiskAdjustment, //TODO we should use the economic basis driver to decide which Economic basis to use
            { ValuationApproach: ValuationApproaches.BBA, IsOci: true } => GetScope<LockedRiskAdjustment>(Identity)
                .LockedRiskAdjustment,
            _ => GetScope<CurrentRiskAdjustment>(Identity).CurrentRiskAdjustment
        };
    }

    public interface LockedRiskAdjustment : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> LockedRiskAdjustment =>
            GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.RA)).FxData
                .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.L));
    }

    public interface CurrentRiskAdjustment : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> CurrentRiskAdjustment =>
            GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.RA)).FxData
                .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.C));
    }

    public interface NominalRiskAdjustment : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> NominalRiskAdjustment =>
            GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.RA)).FxData
                .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.N));
    }

    public interface Fcf : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        private IDataCube<ReportVariable> BestEstimate => GetScope<BestEstimate>(Identity).BestEstimate;
        private IDataCube<ReportVariable> RiskAdjustment => GetScope<RiskAdjustment>(Identity).RiskAdjustment;

        IDataCube<ReportVariable> Fcf => BestEstimate + RiskAdjustment;
    }

    public interface CurrentFcf : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        private IDataCube<ReportVariable> BestEstimate => GetScope<CurrentBestEstimate>(Identity).CurrentBestEstimate;

        private IDataCube<ReportVariable> RiskAdjustment =>
            GetScope<CurrentRiskAdjustment>(Identity).CurrentRiskAdjustment;

        IDataCube<ReportVariable> CurrentFcf => BestEstimate + RiskAdjustment;
    }

    public interface LockedFcf : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        private IDataCube<ReportVariable> BestEstimate => GetScope<LockedBestEstimate>(Identity).LockedBestEstimate;

        private IDataCube<ReportVariable> RiskAdjustment =>
            GetScope<LockedRiskAdjustment>(Identity).LockedRiskAdjustment;

        IDataCube<ReportVariable> LockedFcf => BestEstimate + RiskAdjustment;
    }

    public interface NominalFcf : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        private IDataCube<ReportVariable> BestEstimate => GetScope<NominalBestEstimate>(Identity).NominalBestEstimate;

        private IDataCube<ReportVariable> RiskAdjustment =>
            GetScope<NominalRiskAdjustment>(Identity).NominalRiskAdjustment;

        IDataCube<ReportVariable> NominalFcf => BestEstimate + RiskAdjustment;
    }

    public interface Csm : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> Csm => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.C)).FxData;
    }

    public interface Lc : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> Lc => GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.L)).FxData;
    }

    public interface Loreco : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> Loreco =>
            GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.LR)).FxData;
    }

    public interface WrittenAndAccruals : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> Written =>
            GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.A)).FxData;

        IDataCube<ReportVariable> Advance =>
            GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.AA)).FxData;

        IDataCube<ReportVariable> Overdue =>
            GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.OA)).FxData;
    }

    public interface Deferrals : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> Deferrals =>
            GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.DA)).FxData;
    }

    public interface Revenues : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> Revenues =>
            GetScope<FxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.R)).FxData;
    }

    public interface ExperienceAdjustment : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        private IDataCube<ReportVariable> WrittenCashflow => GetScope<WrittenAndAccruals>(Identity).Written
            .Filter(("VariableType", AocTypes.CF));

        private IDataCube<ReportVariable> BestEstimateCashflow => GetScope<BestEstimate>(Identity).BestEstimate
            .Filter(("VariableType", AocTypes.CF), ("AmountType", "!CDR"))
            .SelectToDataCube(rv => rv with { EconomicBasis = null, Novelty = Novelties.C });

        IDataCube<ReportVariable> ActuarialExperienceAdjustment => WrittenCashflow - BestEstimateCashflow;
    }

    public interface LicActuarial : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        IDataCube<ReportVariable> LicActuarial =>
            GetScope<CurrentFcf>(Identity).CurrentFcf.Filter(("LiabilityType", LiabilityTypes.LIC));
    }

    public interface Lic : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        private IDataCube<ReportVariable> licActuarial => GetScope<LicActuarial>(Identity).LicActuarial;

        private IDataCube<ReportVariable> accrual =>
            GetScope<WrittenAndAccruals>(Identity).Advance.Filter(("LiabilityType", LiabilityTypes.LIC)) +
            GetScope<WrittenAndAccruals>(Identity).Overdue.Filter(("LiabilityType", LiabilityTypes.LIC));

        private IDataCube<ReportVariable> licData => licActuarial + accrual;

        private IDataCube<ReportVariable> bop =>
            licData.Filter(("VariableType", AocTypes.BOP), ("Novelty", Novelties.I));

        private IDataCube<ReportVariable> delta => (licData.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                    licData.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I")))
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(x => Math.Abs(x.Value) >= Consts.Precision,
                x => x with { Novelty = Novelties.C, VariableType = "D" });

        private IDataCube<ReportVariable> eop => licData.Filter(("VariableType", AocTypes.EOP));

        IDataCube<ReportVariable> Lic => bop + delta + eop;
    }

    public interface LrcActuarial : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
            builder.ForScope<LrcActuarial>(s =>
                s.WithApplicability<LrcActuarialPaa>(x => x.Identity.Id.ValuationApproach == ValuationApproaches.PAA));

        private IDataCube<ReportVariable> Fcf =>
            GetScope<CurrentFcf>(Identity).CurrentFcf.Filter(("LiabilityType", LiabilityTypes.LRC));

        private IDataCube<ReportVariable> Csm => GetScope<Csm>(Identity).Csm;
        protected IDataCube<ReportVariable> Loreco => GetScope<Loreco>(Identity).Loreco;

        IDataCube<ReportVariable> LrcActuarial => Fcf + Csm + Loreco;
    }

    public interface LrcActuarialPaa : LrcActuarial
    {
        IDataCube<ReportVariable> LrcActuarial.LrcActuarial =>
            -1d * GetScope<Revenues>(Identity).Revenues + -1d * GetScope<Deferrals>(Identity).Deferrals + Loreco
            + GetScope<BestEstimate>(Identity).BestEstimate
                .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true)
                                .Any(x => x.SystemName == AmountTypes.PR) ||
                            GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true)
                                .Any(x => x.SystemName == AmountTypes.DE))
                .ToDataCube();
    }

    public interface Lrc : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        protected IDataCube<ReportVariable> lrcActuarial => GetScope<LrcActuarial>(Identity).LrcActuarial;

        protected IDataCube<ReportVariable> accrual =>
            GetScope<WrittenAndAccruals>(Identity).Advance.Filter(("LiabilityType", LiabilityTypes.LRC)) +
            GetScope<WrittenAndAccruals>(Identity).Overdue.Filter(("LiabilityType", LiabilityTypes.LRC));

        protected IDataCube<ReportVariable> lrcData => lrcActuarial + accrual;

        private IDataCube<ReportVariable> bop =>
            lrcData.Filter(("VariableType", AocTypes.BOP), ("Novelty", Novelties.I));

        private IDataCube<ReportVariable> delta => (lrcData.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                    lrcData.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I")))
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(x => Math.Abs(x.Value) >= Consts.Precision,
                x => x with { Novelty = Novelties.C, VariableType = "D" });

        private IDataCube<ReportVariable> eop => lrcData.Filter(("VariableType", AocTypes.EOP));

        IDataCube<ReportVariable> Lrc => bop + delta + eop;
    }

    public interface FcfChangeInEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {
        private IDataCube<ReportVariable> FcfDeltas =>
            GetScope<Fcf>(Identity).Fcf.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
            GetScope<Fcf>(Identity).Fcf.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"))
                .Where(x => string.IsNullOrWhiteSpace(x.AmountType)
                    ? true
                    : !GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true)
                        .Any(x => x.SystemName == AmountTypes.DE))
                .ToDataCube();

        private IDataCube<ReportVariable> CurrentFcfDeltas =>
            GetScope<CurrentFcf>(Identity).CurrentFcf.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
            GetScope<CurrentFcf>(Identity).CurrentFcf.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"))
                .Where(x => string.IsNullOrWhiteSpace(x.AmountType)
                    ? true
                    : !GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true)
                        .Any(x => x.SystemName == AmountTypes.DE))
                .ToDataCube();

        // Non-Financial Fp
        private string variableTypeNonFinancial => Identity.Id switch
        {
            { LiabilityType: LiabilityTypes.LRC, IsReinsurance: false } => "IR5",
            { LiabilityType: LiabilityTypes.LRC, IsReinsurance: true } => "ISE10",
            { LiabilityType: LiabilityTypes.LIC } => "ISE12"
        };

        private IDataCube<ReportVariable> NonFinancialFcfDeltas => FcfDeltas
            .Filter(("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"),
                ("VariableType", "!FX"));

        IDataCube<ReportVariable> FpNonFinancial => -1 * NonFinancialFcfDeltas
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = variableTypeNonFinancial });

        // Financial Fp
        private string variableTypeFpFinancial => Identity.Id switch
        {
            { LiabilityType: LiabilityTypes.LRC } => "IFIE1",
            { LiabilityType: LiabilityTypes.LIC } => "IFIE2",
        };

        // OCI 
        private string variableTypeOciFinancial => Identity.Id switch
        {
            { LiabilityType: LiabilityTypes.LRC } => "OCI1",
            { LiabilityType: LiabilityTypes.LIC } => "OCI2",
        };

        private IDataCube<ReportVariable> FinancialFcfDeltas => FcfDeltas.Filter(("VariableType", AocTypes.IA)) +
                                                                FcfDeltas.Filter(("VariableType", AocTypes.YCU)) +
                                                                FcfDeltas.Filter(("VariableType", AocTypes.CRU));

        IDataCube<ReportVariable> FpFx => -1 * FcfDeltas
            .Filter(("VariableType", AocTypes.FX))
            .AggregateOver(nameof(Novelty))
            .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "IFIE3" });

        IDataCube<ReportVariable> FpFinancial => -1 * FinancialFcfDeltas
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = variableTypeFpFinancial });

        IDataCube<ReportVariable> OciFx => (FcfDeltas - CurrentFcfDeltas)
            .Filter(("VariableType", AocTypes.FX))
            .AggregateOver(nameof(Novelty))
            .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "OCI3" });

        IDataCube<ReportVariable> OciFinancial => (FcfDeltas - CurrentFcfDeltas)
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = variableTypeOciFinancial });
    }

    public interface CsmChangeInEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {

        private (string amortization, string nonFinancial) variableType => Identity.Id switch
        {
            { IsReinsurance: false } => ("IR3", "IR5"),
            { IsReinsurance: true } => ("ISE7", "ISE10")
        };

        private IDataCube<ReportVariable> Csm =>
            GetScope<Csm>(Identity).Csm.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
            GetScope<Csm>(Identity).Csm.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

        IDataCube<ReportVariable> Amortization => -1 * Csm.Filter(("VariableType", AocTypes.AM))
            .SelectToDataCube(v => v with { VariableType = variableType.amortization });

        IDataCube<ReportVariable> NonFinancialChanges => -1 * Csm
            .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"),
                ("VariableType", "!CRU"), ("VariableType", "!FX"))
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = variableType.nonFinancial });

        IDataCube<ReportVariable> Fx => -1 * Csm.Filter(("VariableType", AocTypes.FX))
            .AggregateOver(nameof(Novelty))
            .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE3" });

        IDataCube<ReportVariable> FinancialChanges => -1 * (Csm.Filter(("VariableType", AocTypes.IA)) +
                                                            Csm.Filter(("VariableType", AocTypes.YCU)) +
                                                            Csm.Filter(("VariableType", AocTypes.CRU)))
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE1" });
    }

    public interface LcChangeInEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {

        private IDataCube<ReportVariable> Lc =>
            GetScope<Lc>(Identity).Lc.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
            GetScope<Lc>(Identity).Lc.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

        IDataCube<ReportVariable> Amortization => -1 * Lc.Filter(("VariableType", AocTypes.AM))
            .SelectToDataCube(v => v with { VariableType = "ISE9" });

        IDataCube<ReportVariable> NonFinancialChanges => -1 * Lc
            .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"),
                ("VariableType", "!CRU"), ("VariableType", "!FX"))
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

        IDataCube<ReportVariable> NonFinancialChangesToIr => -1 *
                                                             (Amortization + NonFinancialChanges).SelectToDataCube(v =>
                                                                 v with { VariableType = "IR5" });

        IDataCube<ReportVariable> Fx => -1 * Lc.Filter(("VariableType", AocTypes.FX))
            .AggregateOver(nameof(Novelty))
            .SelectToDataCube(v => v with { VariableType = "IFIE3" });

        IDataCube<ReportVariable> FinancialChanges => 1 * (Lc.Filter(("VariableType", AocTypes.IA)) +
                                                           Lc.Filter(("VariableType", AocTypes.YCU)) +
                                                           Lc.Filter(("VariableType", AocTypes.CRU)))
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE1" });

        IDataCube<ReportVariable> FinancialChangesToIse =>
            -1 * FinancialChanges.SelectToDataCube(v => v with { VariableType = "ISE11" });
    }

    public interface LorecoChangeInEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>,
        IDataCube<ReportVariable>
    {

        private IDataCube<ReportVariable> Loreco =>
            GetScope<Loreco>(Identity).Loreco.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
            GetScope<Loreco>(Identity).Loreco.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

        IDataCube<ReportVariable> Amortization => -1 * Loreco.Filter(("VariableType", AocTypes.AM))
            .SelectToDataCube(v => v with { VariableType = "ISE8" });

        IDataCube<ReportVariable> NonFinancialChanges => -1 * Loreco
            .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"),
                ("VariableType", "!CRU"), ("VariableType", "!FX"))
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

        IDataCube<ReportVariable> Fx => -1 * Loreco.Filter(("VariableType", AocTypes.FX))
            .AggregateOver(nameof(Novelty))
            .SelectToDataCube(v => v with { VariableType = "IFIE3" });

        IDataCube<ReportVariable> FinancialChangesToIse => -1 * (Loreco.Filter(("VariableType", AocTypes.IA)) +
                                                                 Loreco.Filter(("VariableType", AocTypes.YCU)) +
                                                                 Loreco.Filter(("VariableType", AocTypes.CRU)))
            .AggregateOver(nameof(Novelty), nameof(VariableType))
            .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });
    }
}

