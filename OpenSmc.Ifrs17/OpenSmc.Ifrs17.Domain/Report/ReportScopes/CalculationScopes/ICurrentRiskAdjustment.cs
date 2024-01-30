using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface ICurrentRiskAdjustment : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    IDataCube<ReportVariable> CurrentRiskAdjustment => GetScope<IFxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.RA)).FxData
        .Filter(("LiabilityType", Identity.Id.LiabilityType), ("EconomicBasis", EconomicBases.C));
}