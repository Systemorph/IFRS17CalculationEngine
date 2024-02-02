using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IInsuranceServiceExpense : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder)
    {
        return builder.ForScope<IInsuranceServiceExpense>(s => s.WithApplicability<IInsuranceServiceExpenseReinsurance>(x => x.Identity.Id.IsReinsurance));
    }

    // Actuals cash flow out Release
    private IDataCube<ReportVariable> WrittenCashflow => GetScope<IWrittenAndAccruals>(Identity).Written.Filter(("VariableType", "CF"));
    private IDataCube<ReportVariable> AdvanceWriteOff => GetScope<IWrittenAndAccruals>(Identity).Advance.Filter(("VariableType", "WO"));
    private IDataCube<ReportVariable> OverdueWriteOff => GetScope<IWrittenAndAccruals>(Identity).Overdue.Filter(("VariableType", "WO"));
    protected IDataCube<ReportVariable> EffectiveActuals => WrittenCashflow - 1 * (AdvanceWriteOff + OverdueWriteOff);

    private IDataCube<ReportVariable> ActualClaims => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.CL))
        .SelectToDataCube(v => v with { VariableType = "ISE2" });

    private IDataCube<ReportVariable> ActualClaimsInvestmentComponent => -1 * EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.ICO))
        .SelectToDataCube(v => v with { VariableType = "ISE5" });

    private IDataCube<ReportVariable> ActualExpenses => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.AE))
        .SelectToDataCube(v => v with { VariableType = "ISE3" });

    private IDataCube<ReportVariable> ActualCommissions => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.AC))
        .SelectToDataCube(v => v with { VariableType = "ISE4" });

    private IDataCube<ReportVariable> ActualClaimExpenses => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(y => y.SystemName == AmountTypes.CE))
        .SelectToDataCube(v => v with { VariableType = "ISE41" });

    // Acquistion Expenses Release (Amortization)
    private IDataCube<ReportVariable> AcquistionExpensesAmortization => GetScope<IDeferrals>(Identity)
        .Filter(("VariableType", AocTypes.AM))
        .SelectToDataCube(v => v with { VariableType = "ISE6" });

    // Financial on IDeferrals Correction
    private IDataCube<ReportVariable> FinancialOnDeferrals => GetScope<IDeferrals>(Identity).Deferrals
        .Filter(x => x.VariableType == AocTypes.IA || x.VariableType == AocTypes.YCU || x.VariableType == AocTypes.CRU || x.VariableType == AocTypes.FX)
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE13" });

    // Loss Component
    private IDataCube<ReportVariable> Lc => GetScope<ILc>(Identity).Lc.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                            GetScope<ILc>(Identity).Lc.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    private IDataCube<ReportVariable> LcAmortization => -1 * Lc.Filter(("VariableType", AocTypes.AM)).SelectToDataCube(v => v with { VariableType = "ISE9" });

    private IDataCube<ReportVariable> LcNonFinancialChanges => -1 * Lc
        .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

    private IDataCube<ReportVariable> LcFinancialChanges => -1 * (Lc.Filter(("VariableType", AocTypes.IA)) +
                                                                  Lc.Filter(("VariableType", AocTypes.YCU)) +
                                                                  Lc.Filter(("VariableType", AocTypes.CRU)))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

    // Change in LIC
    private IDataCube<ReportVariable> FcfDeltas => (GetScope<IFcf>(Identity).Fcf.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                    GetScope<IFcf>(Identity).Fcf.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I")))
        .Filter(("LiabilityType", "LIC")); // TODO, extract the LIC to a dedicated scope (whole thing, actually)

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