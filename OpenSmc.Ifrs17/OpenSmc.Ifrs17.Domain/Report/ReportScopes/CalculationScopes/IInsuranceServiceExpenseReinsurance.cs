using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IInsuranceServiceExpenseReinsurance : IInsuranceServiceExpense
{
    // Expected Best Estimate cash flow out Release
    private IDataCube<ReportVariable> CfOut => -1 * GetScope<IBestEstimate>(Identity).BestEstimate
        .Filter(("VariableType", "CF"), ("LiabilityType", "LRC"))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C });

    private IDataCube<ReportVariable> ExpectedClaims => CfOut // --> Exclude NA Expenses
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.CL))
        .SelectToDataCube(v => v with { VariableType = "ISE22" });

    private IDataCube<ReportVariable> ExpectedClaimsInvestmentComponent => -1 * CfOut
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.ICO))
        .SelectToDataCube(v => v with { VariableType = "ISE23" });

    private IDataCube<ReportVariable> ExpectedExpenses => CfOut
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.AE))
        .SelectToDataCube(v => v with { VariableType = "ISE22" });

    private IDataCube<ReportVariable> ExpectedCommissions => CfOut
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.AC))
        .SelectToDataCube(v => v with { VariableType = "ISE22" });

    // RA Release
    private IDataCube<ReportVariable> RaRelease => -1 * GetScope<IRiskAdjustment>(Identity).RiskAdjustment
        .Filter(("VariableType", "CF"), ("LiabilityType", "LRC"))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "ISE22" });

    // CSM Release (Amortization)
    private IDataCube<ReportVariable> CsmAmortization => -1 * GetScope<ICsm>(Identity).Csm
        .Filter(("VariableType", AocTypes.AM))
        .SelectToDataCube(v => v with { VariableType = "ISE22" });

    // Acquistion Expenses Release (Amortization)
    private IDataCube<ReportVariable> AcquistionExpensesAmortization => Identity.Id switch
    {
        { ValuationApproach: ValuationApproaches.PAA } => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(),
        _ => -1 * GetScope<IDeferrals>(Identity)
            .Filter(("VariableType", AocTypes.AM))
            .SelectToDataCube(v => v with { VariableType = "ISE22" })
    };

    // Financial on IDeferrals Correction
    private IDataCube<ReportVariable> FinancialOnDeferralsToRiRevenue => -1d * GetScope<IDeferrals>(Identity).Deferrals
        .Filter(x => x.VariableType == AocTypes.IA || x.VariableType == AocTypes.YCU || x.VariableType == AocTypes.CRU || x.VariableType == AocTypes.FX)
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE25" });

    // Loss Recovery Component (Amortization)
    private IDataCube<ReportVariable> Loreco => GetScope<ILoreco>(Identity).Loreco.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                GetScope<ILoreco>(Identity).Loreco.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    private IDataCube<ReportVariable> LorecoAmortization => -1 * Loreco
        .Filter(("VariableType", AocTypes.AM))
        .SelectToDataCube(v => v with { VariableType = "ISE8" });

    private IDataCube<ReportVariable> LorecoNonFinancialChanges => -1 * Loreco
        .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

    private IDataCube<ReportVariable> LorecoFinancialChanges => -1 * (Loreco.Filter(("VariableType", AocTypes.IA)) +
                                                                      Loreco.Filter(("VariableType", AocTypes.YCU)) +
                                                                      Loreco.Filter(("VariableType", AocTypes.CRU)))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

    // PAA Premiums
    private IDataCube<ReportVariable> Revenues => GetScope<IRevenues>(Identity).Revenues.Filter(("VariableType", "AM"));

    private IDataCube<ReportVariable> PaaPremiums => Identity.Id switch
    {
        { ValuationApproach: ValuationApproaches.PAA } => Revenues
            .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE201" }),
        _ => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube()
    };

    // Experience Adjustment On Premiums
    private IDataCube<ReportVariable> ReinsuranceActualPremiums => Identity.Id switch
    {
        { ValuationApproach: ValuationApproaches.PAA } => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(),
        _ => EffectiveActuals
            .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.PR))
            .SelectToDataCube(v => v with { Novelty = Novelties.C })
    };

    private IDataCube<ReportVariable> ReinsuranceBestEstimatePremiums => Identity.Id switch
    {
        { ValuationApproach: ValuationApproaches.PAA } => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube(),
        _ => GetScope<IBestEstimate>(Identity).BestEstimate
            .Filter(("VariableType", "CF"), ("LiabilityType", "LRC"))
            .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.PR))
            .AggregateOver(nameof(Novelty))
            .SelectToDataCube(rv => rv with { Novelty = Novelties.C })
    };

    private IDataCube<ReportVariable> ExperienceAdjustmentOnPremiums => (
            ReinsuranceActualPremiums.AggregateOver(nameof(EstimateType)).SelectToDataCube(rv => rv with { EstimateType = EstimateTypes.A }) -
            ReinsuranceBestEstimatePremiums.AggregateOver(nameof(EstimateType)).SelectToDataCube(rv => rv with { EstimateType = EstimateTypes.BE })
        )
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE21" });

    // FCF Locked-In Interest Rate Correction
    private IDataCube<ReportVariable> FcfDeltas => GetScope<IFcf>(Identity).Fcf.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                   GetScope<IFcf>(Identity).Fcf.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    private IDataCube<ReportVariable> LockedFcfDeltas => GetScope<ILockedFcf>(Identity).LockedFcf.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                         GetScope<ILockedFcf>(Identity).LockedFcf.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    private IDataCube<ReportVariable> NonFinancialFcfDeltas => FcfDeltas
        .Filter(("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"));

    private IDataCube<ReportVariable> NonFinancialLockedFcfDeltas => LockedFcfDeltas
        .Filter(("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"));

    private IDataCube<ReportVariable> NonFinancialFcfDeltasCorrection => Identity.Id.LiabilityType == LiabilityTypes.LIC
        ? Enumerable.Empty<ReportVariable>().ToArray().ToDataCube()
        : -1 * (NonFinancialFcfDeltas - NonFinancialLockedFcfDeltas)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "ISE24" });

    // Reinsurance
    IDataCube<ReportVariable> IInsuranceServiceExpense.Reinsurance => ExpectedClaims + ExpectedClaimsInvestmentComponent + ExpectedExpenses + ExpectedCommissions + RaRelease + CsmAmortization + AcquistionExpensesAmortization + FinancialOnDeferralsToRiRevenue + LorecoAmortization + LorecoNonFinancialChanges + LorecoFinancialChanges + PaaPremiums + ExperienceAdjustmentOnPremiums + NonFinancialFcfDeltasCorrection;
}