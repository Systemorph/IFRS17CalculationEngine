using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IExperienceAdjustmentOnPremiumNotApplicable : IExperienceAdjustmentOnPremium
{
    IDataCube<ReportVariable> IExperienceAdjustmentOnPremium.ExperienceAdjustmentOnPremiumTotal => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube();
    IDataCube<ReportVariable> IExperienceAdjustmentOnPremium.ExperienceAdjustmentOnPremiumToCsm => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube();
    IDataCube<ReportVariable> IExperienceAdjustmentOnPremium.ExperienceAdjustmentOnPremiumToRev => Enumerable.Empty<ReportVariable>().ToArray().ToDataCube();
}