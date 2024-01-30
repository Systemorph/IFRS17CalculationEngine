using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IExperienceAdjustmentOnPremium : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder)
    {
        return builder.ForScope<IExperienceAdjustmentOnPremium>(s => s.WithApplicability<IExperienceAdjustmentOnPremiumNotApplicable>(x => x.Identity.Id.IsReinsurance || x.Identity.Id.LiabilityType == LiabilityTypes.LIC));
    }

    private IDataCube<ReportVariable> WrittenPremium => GetScope<IWrittenAndAccruals>(Identity).Written.Filter(("VariableType", "CF"))
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(x => x.SystemName == AmountTypes.PR)).ToDataCube();

    private IDataCube<ReportVariable> BestEstimatePremium => GetScope<IBestEstimate>(Identity).BestEstimate.Filter(("VariableType", "CF"))
        .Where(x => GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, true).Any(x => x.SystemName == AmountTypes.PR)).ToDataCube();

    private IDataCube<ReportVariable> WrittenPremiumToCsm => GetScope<IFxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.APA)).FxData;
    private IDataCube<ReportVariable> BestEstimatePremiumToCsm => GetScope<IFxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.BEPA)).FxData;

    IDataCube<ReportVariable> ExperienceAdjustmentOnPremiumTotal => -1 * (WrittenPremium - BestEstimatePremium)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR77" });

    IDataCube<ReportVariable> ExperienceAdjustmentOnPremiumToCsm => (WrittenPremiumToCsm.SelectToDataCube(v => v with { EstimateType = EstimateTypes.BE }) - BestEstimatePremiumToCsm.SelectToDataCube(v => v with { EstimateType = EstimateTypes.A }))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR78" });

    IDataCube<ReportVariable> ExperienceAdjustmentOnPremiumToRev => ((WrittenPremium - WrittenPremiumToCsm).AggregateOver(nameof(EstimateType)).SelectToDataCube(v => v with { EstimateType = EstimateTypes.A })
                                                                     - (BestEstimatePremium - BestEstimatePremiumToCsm).AggregateOver(nameof(EstimateType)).SelectToDataCube(v => v with { EstimateType = EstimateTypes.BE }))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR79" });
}