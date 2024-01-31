using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.WrittenActualCalculation;

public interface IWrittenActual : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IWrittenActual>(s => s
            .WithApplicability<IActualEmptyValue>(x => !(x.Identity.Id.AocType == AocTypes.CF && x.Identity.Id.Novelty == Novelties.C))
            .WithApplicability<IActualProjection>(x => !x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode) && x.Identity.Id.ProjectionPeriod > 0)
            .WithApplicability<IActualFromPaymentPattern>(x => !x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode) && Math.Abs(x.Value) < Consts.Precision));

    double Value => GetStorage().GetValue(Identity.Id, Identity.AmountType, Identity.EstimateType, Identity.AccidentYear, Identity.Id.ProjectionPeriod);
}