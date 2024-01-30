using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IDataWrittenActual : IDataScope
{
    IDataCube<ReportVariable> IDataScope.Data => RawData;
}