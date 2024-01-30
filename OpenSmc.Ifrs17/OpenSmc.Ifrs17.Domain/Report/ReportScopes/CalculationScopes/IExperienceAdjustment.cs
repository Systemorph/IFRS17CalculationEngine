using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IExperienceAdjustment : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> WrittenCashflow => GetScope<IWrittenAndAccruals>(Identity).Written
        .Filter(("VariableType", AocTypes.CF));

    private IDataCube<ReportVariable> BestEstimateCashflow => GetScope<IBestEstimate>(Identity).BestEstimate
        .Filter(("VariableType", AocTypes.CF), ("AmountType", "!CDR"))
        .SelectToDataCube(rv => rv with { EconomicBasis = null, Novelty = Novelties.C });

    IDataCube<ReportVariable> ActuarialExperienceAdjustment => WrittenCashflow - BestEstimateCashflow;
}