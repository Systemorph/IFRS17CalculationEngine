using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IInsuranceRevenueNotApplicable : IInsuranceRevenue
{
    IDataCube<ReportVariable> IInsuranceRevenue.InsuranceRevenue => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube();
}