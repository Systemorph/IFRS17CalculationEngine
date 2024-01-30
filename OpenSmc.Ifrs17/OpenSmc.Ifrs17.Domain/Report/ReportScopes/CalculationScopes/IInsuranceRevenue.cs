using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IInsuranceRevenue : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder)
    {
        return builder.ForScope<IInsuranceRevenue>(s => s.WithApplicability<IInsuranceRevenueNotApplicable>(x => x.Identity.Id.IsReinsurance || x.Identity.Id.LiabilityType == LiabilityTypes.LIC));
    }

    // PAA Premiums
    private IDataCube<ReportVariable> WrittenCashflow => GetScope<IWrittenAndAccruals>(Identity).Written.Filter(("VariableType", "CF"));
    private IDataCube<ReportVariable> AdvanceWriteOff => GetScope<IWrittenAndAccruals>(Identity).Advance.Filter(("VariableType", "WO"));
    private IDataCube<ReportVariable> OverdueWriteOff => GetScope<IWrittenAndAccruals>(Identity).Overdue.Filter(("VariableType", "WO"));
    private IDataCube<ReportVariable> EffectiveActuals => WrittenCashflow - 1 * (AdvanceWriteOff + OverdueWriteOff);
    private IDataCube<ReportVariable> Revenues => GetScope<IRevenues>(Identity).Revenues.Filter(("VariableType", "AM"));

    private IDataCube<ReportVariable> PaaPremiums => Identity.Id switch
    {
        { ValuationApproach: ValuationApproaches.PAA } => Revenues
            .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR11" }),
        _ => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube()
    };

    // Experience Adjustment On Premiums
    private IDataCube<ReportVariable> NotPaaActualPremiums => Identity.Id switch
    {
        { ValuationApproach: ValuationApproaches.PAA } => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(),
        _ => EffectiveActuals
            .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.PR))
            .SelectToDataCube(v => v with { Novelty = Novelties.C })
    };

    private IDataCube<ReportVariable> NotPaaBestEstimatePremiums => Identity.Id switch
    {
        { ValuationApproach: ValuationApproaches.PAA } => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(),
        _ => GetScope<IBestEstimate>(Identity).BestEstimate
            .Filter(("VariableType", "CF"), ("LiabilityType", "LRC"))
            .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.PR))
            .AggregateOver(nameof(Novelty))
            .SelectToDataCube(rv => rv with { Novelty = Novelties.C })
    };

    private IDataCube<ReportVariable> WrittenPremiumsToCsm => GetScope<IFxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.APA)).FxData;
    private IDataCube<ReportVariable> BestEstimatePremiumsToCsm => GetScope<IFxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.BEPA)).FxData;

    private IDataCube<ReportVariable> ExperienceAdjustmentOnPremiums => (
            (NotPaaActualPremiums - WrittenPremiumsToCsm).AggregateOver(nameof(EstimateType)).SelectToDataCube(rv => rv with { EstimateType = EstimateTypes.A }) -
            (NotPaaBestEstimatePremiums - BestEstimatePremiumsToCsm).AggregateOver(nameof(EstimateType)).SelectToDataCube(rv => rv with { EstimateType = EstimateTypes.BE })
        )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR12" });

    // Expected Best Estimate cash flow out Release
    private IDataCube<ReportVariable> CfOut => -1 * GetScope<IBestEstimate>(Identity).BestEstimate
        .Filter(("VariableType", "CF"), ("LiabilityType", "LRC"))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C });

    private IDataCube<ReportVariable> ExpectedClaims => CfOut // --> Exclude NA Expenses
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.CL))
        .SelectToDataCube(v => v with { VariableType = "IR13" });

    private IDataCube<ReportVariable> ExpectedClaimsInvestmentComponent => -1 * CfOut
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.ICO))
        .SelectToDataCube(v => v with { VariableType = "IR2" });

    private IDataCube<ReportVariable> ExpectedExpenses => CfOut
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.AE))
        .SelectToDataCube(v => v with { VariableType = "IR13" });

    private IDataCube<ReportVariable> ExpectedCommissions => CfOut
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.AC))
        .SelectToDataCube(v => v with { VariableType = "IR13" });

    // RA Release
    private IDataCube<ReportVariable> RaRelease => -1 * GetScope<IRiskAdjustment>(Identity).RiskAdjustment
        .Filter(("VariableType", "CF"), ("LiabilityType", "LRC"))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "IR13" });

    // CSM Release (Amortization)
    private IDataCube<ReportVariable> CsmAmortization => -1 * GetScope<ICsm>(Identity).Csm
        .Filter(("VariableType", AocTypes.AM))
        .SelectToDataCube(v => v with { VariableType = "IR13" });

    // Loss Component Release (Amortization)
    private IDataCube<ReportVariable> LossComponentAmortization => GetScope<ILc>(Identity).Lc
        .Filter(("VariableType", AocTypes.AM))
        .SelectToDataCube(v => v with { VariableType = "IR13" });

    // Acquistion Expenses Release (Amortization)
    private IDataCube<ReportVariable> AcquistionExpensesAmortization => Identity.Id switch
    {
        { ValuationApproach: ValuationApproaches.PAA } => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(),
        _ => -1 * GetScope<IDeferrals>(Identity)
            .Filter(("VariableType", AocTypes.AM))
            .SelectToDataCube(v => v with { VariableType = "IR13" })
    };

    // Financial on IDeferrals Correction
    private IDataCube<ReportVariable> FinancialOnDeferrals => -1 * GetScope<IDeferrals>(Identity).Deferrals
        .Filter(x => x.VariableType == AocTypes.IA || x.VariableType == AocTypes.YCU || x.VariableType == AocTypes.CRU || x.VariableType == AocTypes.FX)
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR9" });

    // FCF Locked-In Interest Rate Correction
    private IDataCube<ReportVariable> FcfDeltas => GetScope<IFcf>(Identity).Fcf.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                   GetScope<IFcf>(Identity).Fcf.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    private IDataCube<ReportVariable> LockedFcfDeltas => GetScope<ILockedFcf>(Identity).LockedFcf.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                         GetScope<ILockedFcf>(Identity).LockedFcf.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    private IDataCube<ReportVariable> NonFinancialFcfDeltas => FcfDeltas
        .Filter(("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"));

    private IDataCube<ReportVariable> NonFinancialLockedFcfDeltas => LockedFcfDeltas
        .Filter(("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"));

    private IDataCube<ReportVariable> NonFinancialFcfDeltasCorrection => -1 * (NonFinancialFcfDeltas - NonFinancialLockedFcfDeltas)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "IR14" });

    // IInsuranceRevenue   
    IDataCube<ReportVariable> InsuranceRevenue => PaaPremiums + ExperienceAdjustmentOnPremiums + RaRelease + CsmAmortization + LossComponentAmortization + ExpectedClaims + ExpectedClaimsInvestmentComponent + ExpectedExpenses + ExpectedCommissions + AcquistionExpensesAmortization + FinancialOnDeferrals + NonFinancialFcfDeltasCorrection;
}