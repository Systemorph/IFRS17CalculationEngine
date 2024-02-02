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

public interface IInsuranceFinanceIncomeExpenseOci : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    //FCF
    private IDataCube<ReportVariable> FcfDeltas => GetScope<IFcf>(Identity).Fcf.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                   GetScope<IFcf>(Identity).Fcf.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    private IDataCube<ReportVariable> CurrentFcfDeltas => Identity.Id switch
    {
        { ValuationApproach: ValuationApproaches.PAA, LiabilityType: LiabilityTypes.LRC } => FcfDeltas,
        _ => GetScope<ICurrentFcf>(Identity).CurrentFcf.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
             GetScope<ICurrentFcf>(Identity).CurrentFcf.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"))
    };

    // Financial Fp
    private string VariableTypeFpFinancial => Identity.Id switch
    {
        { LiabilityType: LiabilityTypes.LRC } => "IFIE1",
        { LiabilityType: LiabilityTypes.LIC } => "IFIE2",
        _ => throw new ArgumentOutOfRangeException()
    };

    // OCI 
    private string VariableTypeOciFinancial => Identity.Id switch
    {
        { LiabilityType: LiabilityTypes.LRC } => "OCI1",
        { LiabilityType: LiabilityTypes.LIC } => "OCI2",
        _ => throw new ArgumentOutOfRangeException()
    };

    private IDataCube<ReportVariable> FinancialFcfDeltas => FcfDeltas.Filter(("VariableType", AocTypes.IA)) +
                                                            FcfDeltas.Filter(("VariableType", AocTypes.YCU)) +
                                                            FcfDeltas.Filter(("VariableType", AocTypes.CRU));

    private IDataCube<ReportVariable> FpFcfFx => -1 * FcfDeltas
        .Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "IFIE3" });

    private IDataCube<ReportVariable> FpFcfFinancial => -1 * FinancialFcfDeltas
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = VariableTypeFpFinancial });

    private IDataCube<ReportVariable> OciFcfFx => (FcfDeltas - CurrentFcfDeltas)
        .Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "OCI3" });

    private IDataCube<ReportVariable> OciFcfFinancial => (FcfDeltas - CurrentFcfDeltas)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = VariableTypeOciFinancial });

    // CSM
    private IDataCube<ReportVariable> Csm => GetScope<ICsm>(Identity).Csm.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                             GetScope<ICsm>(Identity).Csm.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    private IDataCube<ReportVariable> CsmFx => -1 * Csm.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE3" });

    private IDataCube<ReportVariable> CsmFinancialChanges => -1 * (Csm.Filter(("VariableType", AocTypes.IA)) +
                                                                   Csm.Filter(("VariableType", AocTypes.YCU)) +
                                                                   Csm.Filter(("VariableType", AocTypes.CRU)))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE1" });

    // LC
    private IDataCube<ReportVariable> Lc => GetScope<ILc>(Identity).Lc.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                            GetScope<ILc>(Identity).Lc.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    private IDataCube<ReportVariable> LcFx => -1 * Lc.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { VariableType = "IFIE3" });

    private IDataCube<ReportVariable> LcFinancialChanges => 1 * (Lc.Filter(("VariableType", AocTypes.IA)) +
                                                                 Lc.Filter(("VariableType", AocTypes.YCU)) +
                                                                 Lc.Filter(("VariableType", AocTypes.CRU)))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE1" });

    // LoReCo
    private IDataCube<ReportVariable> Loreco => GetScope<ILoreco>(Identity).Loreco.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                GetScope<ILoreco>(Identity).Loreco.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    private IDataCube<ReportVariable> LorecoFx => -1 * Loreco.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { VariableType = "IFIE3" });

    // PAA IRevenues
    private IDataCube<ReportVariable> PaaRevenue => GetScope<IRevenues>(Identity).Revenues.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                    GetScope<IRevenues>(Identity).Revenues.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    private IDataCube<ReportVariable> PaaRevenueFx => -1 * PaaRevenue.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { VariableType = "IFIE3" });

    private IDataCube<ReportVariable> PaaRevenueFinancialChanges => 1 * (PaaRevenue.Filter(("VariableType", AocTypes.IA)) +
                                                                         PaaRevenue.Filter(("VariableType", AocTypes.YCU)) +
                                                                         PaaRevenue.Filter(("VariableType", AocTypes.CRU)))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = VariableTypeFpFinancial });

    // PAA IDeferrals
    private IDataCube<ReportVariable> PaaDeferrals => Identity.Id switch
    {
        { ValuationApproach: ValuationApproaches.PAA, LiabilityType: LiabilityTypes.LRC }
            => GetScope<IDeferrals>(Identity).Deferrals.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
               GetScope<IDeferrals>(Identity).Deferrals.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I")),
        _ => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube()
    };

    private IDataCube<ReportVariable> PaaDeferralsFx => -1 * PaaDeferrals.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { VariableType = "IFIE3" });

    private IDataCube<ReportVariable> PaaDeferralsFinancialChanges => 1 * (PaaDeferrals.Filter(("VariableType", AocTypes.IA)) +
                                                                           PaaDeferrals.Filter(("VariableType", AocTypes.YCU)) +
                                                                           PaaDeferrals.Filter(("VariableType", AocTypes.CRU)))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = VariableTypeFpFinancial });

    //Insurance Finance Income/Expense Oci
    IDataCube<ReportVariable> InsuranceFinanceIncomeExpenseOci => FpFcfFx + FpFcfFinancial + OciFcfFx + OciFcfFinancial + CsmFx + CsmFinancialChanges + LcFx + LcFinancialChanges + LorecoFx
                                                                  + PaaRevenueFinancialChanges + PaaRevenueFx + PaaDeferralsFinancialChanges + PaaDeferralsFx;
}