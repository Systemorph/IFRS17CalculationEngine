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

public interface IFcfChangeInEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> FcfDeltas => GetScope<IFcf>(Identity).Fcf.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                   GetScope<IFcf>(Identity).Fcf.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"))
                                                       .Where(x => string.IsNullOrWhiteSpace(x.AmountType) ? true : !GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(x => x.SystemName == AmountTypes.DE))
                                                       .ToDataCube();

    private IDataCube<ReportVariable> CurrentFcfDeltas => GetScope<ICurrentFcf>(Identity).CurrentFcf.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                          GetScope<ICurrentFcf>(Identity).CurrentFcf.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"))
                                                              .Where(x => string.IsNullOrWhiteSpace(x.AmountType) ? true : !GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(x => x.SystemName == AmountTypes.DE))
                                                              .ToDataCube();

    // Non-Financial Fp
    private string VariableTypeNonFinancial => Identity.Id switch
    {
        { LiabilityType: LiabilityTypes.LRC, IsReinsurance: false } => "IR5",
        { LiabilityType: LiabilityTypes.LRC, IsReinsurance: true } => "ISE10",
        { LiabilityType: LiabilityTypes.LIC } => "ISE12"
    };

    private IDataCube<ReportVariable> NonFinancialFcfDeltas => FcfDeltas
        .Filter(("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"));

    IDataCube<ReportVariable> FpNonFinancial => -1 * NonFinancialFcfDeltas
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = VariableTypeNonFinancial });

    // Financial Fp
    private string VariableTypeFpFinancial => Identity.Id switch
    {
        { LiabilityType: LiabilityTypes.LRC } => "IFIE1",
        { LiabilityType: LiabilityTypes.LIC } => "IFIE2"
    };

    // OCI 
    private string VariableTypeOciFinancial => Identity.Id switch
    {
        { LiabilityType: LiabilityTypes.LRC } => "OCI1",
        { LiabilityType: LiabilityTypes.LIC } => "OCI2"
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
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = VariableTypeFpFinancial });

    IDataCube<ReportVariable> OciFx => (FcfDeltas - CurrentFcfDeltas)
        .Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = "OCI3" });

    IDataCube<ReportVariable> OciFinancial => (FcfDeltas - CurrentFcfDeltas)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(rv => rv with { Novelty = Novelties.C, VariableType = VariableTypeOciFinancial });
}