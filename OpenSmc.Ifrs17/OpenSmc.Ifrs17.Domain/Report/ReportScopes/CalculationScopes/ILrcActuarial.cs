using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface ILrcActuarial : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder)
    {
        return builder.ForScope<ILrcActuarial>(s => s.WithApplicability<ILrcActuarialPaa>(x => x.Identity.Id.ValuationApproach == ValuationApproaches.PAA));
    }

    private IDataCube<ReportVariable> Fcf => GetScope<ICurrentFcf>(Identity).CurrentFcf.Filter(("LiabilityType", LiabilityTypes.LRC));
    private IDataCube<ReportVariable> Csm => GetScope<ICsm>(Identity).Csm;
    protected IDataCube<ReportVariable> Loreco => GetScope<ILoreco>(Identity).Loreco;

    IDataCube<ReportVariable> LrcActuarial => Fcf + Csm + Loreco;
}