using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.AccrualActual;

public interface IAccrualActual : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IAccrualActual>(s => s
            .WithApplicability<IAccrualEmptyValues>(x => !x.GetStorage().GetAllAocSteps(StructureType.AocAccrual).Contains(x.Identity.Id.AocStep))
            .WithApplicability<IAccrualEndOfPeriod>(x => !x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode) && x.Identity.Id.AocType == AocTypes.EOP)
            .WithApplicability<IAccrualEmptyValues>(x => !x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode) && x.Identity.Id.ProjectionPeriod > x.GetStorage().FirstNextYearProjection) // projections beyond ReportingYear +1
            .WithApplicability<IAccrualProjectionFirstYear>(x => !x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode) && x.Identity.Id.ProjectionPeriod == x.GetStorage().FirstNextYearProjection && // projections ReportingYear +1
                                                                 (x.Identity.Id.AocType == AocTypes.BOP || x.Identity.Id.AocType == AocTypes.CF))
            .WithApplicability<IAccrualEmptyValues>(x => x.Identity.Id.ProjectionPeriod == x.GetStorage().FirstNextYearProjection && x.Identity.Id.AocType == AocTypes.WO)
            .WithApplicability<IAccrualProjectionWithinFirstYear>(x => !x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode) && x.Identity.Id.ProjectionPeriod > 0)
        );

    public double Value => GetStorage().GetValue(Identity.Id, Identity.AmountType, Identity.EstimateType, Identity.AccidentYear, Identity.Id.ProjectionPeriod);
}