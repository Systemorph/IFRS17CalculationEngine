using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginAllocation;

public interface IAllocateTechnicalMargin : IScope<ImportIdentity, ImportStorageOld>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IAllocateTechnicalMargin>(s => s
            .WithApplicability<IComputeIAllocateTechnicalMarginWithIfrsVariable>(x => x.GetStorage().IsSecondaryScope(x.Identity.DataNode))
            .WithApplicability<IAllocateTechnicalMarginForBop>(x => x.Identity.AocType == AocTypes.BOP)
            .WithApplicability<IAllocateTechnicalMarginForCl>(x => x.Identity.AocType == AocTypes.CL)
            .WithApplicability<IAllocateTechnicalMarginForEop>(x => x.Identity.AocType == AocTypes.EOP)
        );

    [NotVisible] double AggregatedTechnicalMargin => GetScope<ITechnicalMargin>(Identity).AggregatedValue;
    [NotVisible] double TechnicalMargin => GetScope<ITechnicalMargin>(Identity).Value;
    [NotVisible] string ComputedEstimateType => ComputeEstimateType(GetScope<ITechnicalMargin>(Identity).AggregatedValue + TechnicalMargin);
    [NotVisible] bool HasSwitch => ComputedEstimateType != ComputeEstimateType(GetScope<ITechnicalMargin>(Identity).AggregatedValue);

    [NotVisible] string EstimateType => GetContext();

    double Value => (HasSwitch, EstimateType == ComputedEstimateType) switch
    {
        (true, true) => TechnicalMargin + AggregatedTechnicalMargin,
        (true, false) => -1d * AggregatedTechnicalMargin,
        (false, true) => TechnicalMargin,
        _ => default
    };

    string ComputeEstimateType(double aggregatedTechnicalMargin) => aggregatedTechnicalMargin > Consts.Precision ? EstimateTypes.L : EstimateTypes.C;
}