using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IIncurredActuals : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> WrittenCashflow => GetScope<IWrittenAndAccruals>(Identity).Written.Filter(("VariableType", "CF"));
    private IDataCube<ReportVariable> AdvanceWriteOff => GetScope<IWrittenAndAccruals>(Identity).Advance.Filter(("VariableType", "WO"));
    private IDataCube<ReportVariable> OverdueWriteOff => GetScope<IWrittenAndAccruals>(Identity).Overdue.Filter(("VariableType", "WO"));
    private IDataCube<ReportVariable> EffectiveActuals => WrittenCashflow - 1 * (AdvanceWriteOff + OverdueWriteOff);

    private string PremiumsVariableType => Identity.Id switch
    {
        { IsReinsurance: false } => "IR1",
        { IsReinsurance: true } => "ISE1"
    };

    IDataCube<ReportVariable> Premiums => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(x => x.SystemName == AmountTypes.PR))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = PremiumsVariableType });

    IDataCube<ReportVariable> Claims => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(x => x.SystemName == AmountTypes.CL))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE2" });

    private IDataCube<ReportVariable> ClaimsIco => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(x => x.SystemName == AmountTypes.ICO)).ToDataCube();

    IDataCube<ReportVariable> ClaimsIcoToIr => ClaimsIco.SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR2" }); //TODO, add Reinsurance case
    IDataCube<ReportVariable> ClaimsIcoToIse => (-1 * ClaimsIco).SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE5" });

    IDataCube<ReportVariable> Expenses => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(x => x.SystemName == AmountTypes.AE))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE3" });

    IDataCube<ReportVariable> Commissions => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(x => x.SystemName == AmountTypes.AC))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE4" });

    IDataCube<ReportVariable> ClaimExpenses => EffectiveActuals
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(x => x.SystemName == AmountTypes.CE))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE41" });
}