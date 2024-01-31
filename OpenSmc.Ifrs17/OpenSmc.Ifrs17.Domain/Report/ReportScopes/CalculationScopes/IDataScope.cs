using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Scopes;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IDataScope : IScope<(ReportIdentity ReportIdentity, string EstimateType), ReportStorage>, IDataCube<ReportVariable>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder)
    {
        return builder.ForScope<IDataScope>(s => s.WithApplicability<IDataWrittenActual>(x => x.GetStorage().EstimateTypesWithoutAoc.Contains(x.Identity.EstimateType)));
    }

    protected IDataCube<ReportVariable> RawData => GetStorage().GetVariables(Identity.ReportIdentity, Identity.EstimateType);

    private IDataCube<ReportVariable> RawEops => RawData.Filter(("VariableType", AocTypes.EOP));
    private IDataCube<ReportVariable> NotEopsNotCls => RawData.Filter(("VariableType", "!EOP"), ("VariableType", "!CL")); // TODO negation must be hardcoded (also to avoid string concatenation)

    private IDataCube<ReportVariable> CalculatedCl => (RawEops - NotEopsNotCls)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(x => Math.Abs(x.Value) >= Consts.Precision, x => x with { Novelty = Novelties.C, VariableType = AocTypes.CL });

    private IDataCube<ReportVariable> CalculatedEops => (NotEopsNotCls + CalculatedCl)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(x => x with { VariableType = AocTypes.EOP, Novelty = Novelties.C });

    IDataCube<ReportVariable> Data => NotEopsNotCls + CalculatedCl + CalculatedEops;
}