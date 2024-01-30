using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface ILrcTechnicalMargin : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> Csm => GetScope<ICsm>(Identity).Csm;
    private IDataCube<ReportVariable> Lc => GetScope<ILc>(Identity).Lc;
    private IDataCube<ReportVariable> Loreco => GetScope<ILoreco>(Identity).Loreco;

    IDataCube<ReportVariable> LrcTechnicalMargin => Lc + Loreco - 1 * Csm;
}