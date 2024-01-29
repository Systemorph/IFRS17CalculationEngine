using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface INominalCashflow : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<INominalCashflow>(s => s
            .WithApplicability<EmptyINominalCashflow>(x =>
                    (x.Identity.Id.AocType != AocTypes.CL && x.Identity.Id.AocType != AocTypes.EOP) && // if AocType is NOT CL AND NOT EOP AND
                    x.Identity.Id.Novelty != Novelties.I && // if Novelty is NOT inforce AND
                    x.Identity.Id.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection && // if it is projection >= 1 Year AND
                    !(x.Identity.AccidentYear.HasValue && Consts.MonthInAYear * x.Identity.AccidentYear >= (Consts.MonthInAYear * x.GetStorage().CurrentReportingPeriod.Year + x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod))) // if it DOES NOT (have AY and with AY >= than projected FY)
            )
            .WithApplicability<EmptyINominalCashflow>(x =>
                    (x.Identity.Id.AocType == AocTypes.BOP || x.Identity.Id.AocType == AocTypes.CF || x.Identity.Id.AocType == AocTypes.IA) && // if AocType is BOP, CF or IA (or not in telescopic) AND
                    x.Identity.Id.Novelty == Novelties.I && // if Novelty is inforce AND
                    x.Identity.Id.LiabilityType == LiabilityTypes.LIC && // if LiabilityType is LIC AND
                    x.Identity.Id.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection && // if it is projection >= 1 Year AND
                    (x.Identity.AccidentYear.HasValue && Consts.MonthInAYear * x.Identity.AccidentYear >= (Consts.MonthInAYear * x.GetStorage().CurrentReportingPeriod.Year + x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod))) // if it DOES (have AY and with AY >= than projected FY)
            )
            .WithApplicability<EmptyINominalCashflow>(x =>
                    x.Identity.Id.LiabilityType == LiabilityTypes.LRC && // if LiabilityType is LRC
                    x.Identity.Id.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection && // if it is projection >= 1 Year AND
                    (x.Identity.AccidentYear.HasValue && Consts.MonthInAYear * x.Identity.AccidentYear < (Consts.MonthInAYear * x.GetStorage().CurrentReportingPeriod.Year + x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod))) // if it DOES (have AY and with AY < than projected FY)
            )
            .WithApplicability<EmptyINominalCashflow>(x =>
                    (x.Identity.Id.AocType == AocTypes.BOP || x.Identity.Id.AocType == AocTypes.CF || x.Identity.Id.AocType == AocTypes.IA) && // if AocType is BOP, CF or IA (or not in telescopic) AND
                    (x.Identity.Id.Novelty != Novelties.I && x.Identity.Id.Novelty != Novelties.C) && // if Novelty is NOT inforce AND
                    x.Identity.Id.LiabilityType == LiabilityTypes.LRC && // if LiabilityType is LRC AND
                    x.Identity.Id.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection && // if it is projection >= 1 Year AND
                    (x.Identity.AccidentYear.HasValue && Consts.MonthInAYear * x.Identity.AccidentYear >= (Consts.MonthInAYear * x.GetStorage().CurrentReportingPeriod.Year + x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod))) // if it DOES (have AY and with AY >= than projected FY)
            )
            .WithApplicability<EmptyINominalCashflow>(x =>
                    (x.Identity.Id.AocType == AocTypes.CF) && // if AocType is CF AND
                    x.Identity.Id.LiabilityType == LiabilityTypes.LRC && x.Identity.AccidentYear.HasValue && // if LiabilityType is LRC with AY defined
                    x.Identity.Id.ProjectionPeriod < x.GetStorage().FirstNextYearProjection && //  if it is projection == 0 AND
                    //x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod) == 0 && // if it is projection == 0 AND
                    !(Consts.MonthInAYear * x.Identity.AccidentYear == (Consts.MonthInAYear * x.GetStorage().CurrentReportingPeriod.Year + x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod))) // if AY == projected FY
            )
            .WithApplicability<CreditDefaultRiskINominalCashflow>(x => x.GetStorage().GetCdr().Contains(x.Identity.AmountType) && x.Identity.Id.AocType == AocTypes.CF)
            .WithApplicability<AllClaimsCashflow>(x => x.GetStorage().GetCdr().Contains(x.Identity.AmountType))
        );

    IEnumerable<AocStep> ReferenceAocSteps => GetScope<IReferenceAocStep>(Identity.Id).Values;
    double[] Values => ReferenceAocSteps.Select(refAocStep => GetStorage().GetValues(Identity.Id with {AocType = refAocStep.AocType, Novelty = refAocStep.Novelty}, Identity.AmountType, Identity.EstimateType, Identity.AccidentYear))
        .AggregateDoubleArray();
}