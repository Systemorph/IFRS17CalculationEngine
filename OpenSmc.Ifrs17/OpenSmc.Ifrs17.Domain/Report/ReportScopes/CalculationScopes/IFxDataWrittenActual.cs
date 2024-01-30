using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IFxDataWrittenActual : IFxData
{
    IDataCube<ReportVariable> IFxData.FxData => Data;
}