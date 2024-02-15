using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Scopes;
using OpenSms.Ifrs17.CalculationScopes.AocSteps;

namespace OpenSms.Ifrs17.CalculationScopes.NominalCashflow;

public interface INominalCashflow : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    int FirstNextYearProjection() => GetScope<IProjectionConfiguration>().FirstNextYearProjection(); 
    int ReportingPeriodYear() => 2020; // Nesd logic

    string[] Cdr() => GetStorage().GetAmountTypes()
        .Where(x => x.SystemName == AmountTypes.CDR) // Include descendants here
        .Select(x => x.SystemName).ToArray();

    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<INominalCashflow>(s => s
            .WithApplicability<IEmptyINominalCashflow>(x =>
                    x.Identity.Id.AocType != AocTypes.CL && x.Identity.Id.AocType != AocTypes.EOP && // if AocType is NOT CL AND NOT EOP AND
                    x.Identity.Id.Novelty != Novelties.I && // if Novelty is NOT inforce AND
                    x.Identity.Id.ProjectionPeriod >= x.FirstNextYearProjection() && // if it is projection >= 1 Year AND
                    !(x.Identity.AccidentYear.HasValue && Consts.MonthInAYear * x.Identity.AccidentYear >= Consts.MonthInAYear * x.ReportingPeriodYear()) //+ x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod)) // if it DOES NOT (have AY and with AY >= than projected FY)
            )
            .WithApplicability<IEmptyINominalCashflow>(x =>
                    (x.Identity.Id.AocType == AocTypes.BOP || x.Identity.Id.AocType == AocTypes.CF || x.Identity.Id.AocType == AocTypes.IA) && // if AocType is BOP, CF or IA (or not in telescopic) AND
                    x.Identity.Id.Novelty == Novelties.I && // if Novelty is inforce AND
                    x.Identity.Id.LiabilityType == LiabilityTypes.LIC && // if LiabilityType is LIC AND
                    x.Identity.Id.ProjectionPeriod >= x.FirstNextYearProjection() && // if it is projection >= 1 Year AND
                    x.Identity.AccidentYear.HasValue && Consts.MonthInAYear * x.Identity.AccidentYear >= Consts.MonthInAYear * x.ReportingPeriodYear() //+ x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod) // if it DOES (have AY and with AY >= than projected FY)
            )
            .WithApplicability<IEmptyINominalCashflow>(x =>
                    x.Identity.Id.LiabilityType == LiabilityTypes.LRC && // if LiabilityType is LRC
                    x.Identity.Id.ProjectionPeriod >= x.FirstNextYearProjection() && // if it is projection >= 1 Year AND
                    x.Identity.AccidentYear.HasValue && Consts.MonthInAYear * x.Identity.AccidentYear < Consts.MonthInAYear * x.ReportingPeriodYear() //+ x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod) // if it DOES (have AY and with AY < than projected FY)
            )
            .WithApplicability<IEmptyINominalCashflow>(x =>
                    (x.Identity.Id.AocType == AocTypes.BOP || x.Identity.Id.AocType == AocTypes.CF || x.Identity.Id.AocType == AocTypes.IA) && // if AocType is BOP, CF or IA (or not in telescopic) AND
                    x.Identity.Id.Novelty != Novelties.I && x.Identity.Id.Novelty != Novelties.C && // if Novelty is NOT inforce AND
                    x.Identity.Id.LiabilityType == LiabilityTypes.LRC && // if LiabilityType is LRC AND
                    x.Identity.Id.ProjectionPeriod >= x.FirstNextYearProjection() && // if it is projection >= 1 Year AND
                    x.Identity.AccidentYear.HasValue && Consts.MonthInAYear * x.Identity.AccidentYear >= Consts.MonthInAYear * x.ReportingPeriodYear()// + x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod) // if it DOES (have AY and with AY >= than projected FY)
            )
            .WithApplicability<IEmptyINominalCashflow>(x =>
                    x.Identity.Id.AocType == AocTypes.CF && // if AocType is CF AND
                    x.Identity.Id.LiabilityType == LiabilityTypes.LRC && x.Identity.AccidentYear.HasValue && // if LiabilityType is LRC with AY defined
                    x.Identity.Id.ProjectionPeriod < x.FirstNextYearProjection() && //  if it is projection == 0 AND
                                                                                               //x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod) == 0 && // if it is projection == 0 AND
                    !(Consts.MonthInAYear * x.Identity.AccidentYear == Consts.MonthInAYear * x.ReportingPeriodYear())// + x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod)) // if AY == projected FY
            )
            .WithApplicability<ICreditDefaultRiskINominalCashflow>(x => x.Cdr().Contains(x.Identity.AmountType) && x.Identity.Id.AocType == AocTypes.CF)
            .WithApplicability<IAllClaimsCashflow>(x => x.Cdr().Contains(x.Identity.AmountType))
        );

    IEnumerable<AocStep> ReferenceAocSteps => GetScope<IReferenceAocStep>(Identity.Id).Values;
    double[] Values => ReferenceAocSteps.Select(refAocStep => GetScope<IRawVariable>((Identity.Id with { AocType = refAocStep.AocType, Novelty = refAocStep.Novelty }, Identity.AmountType, Identity.EstimateType, Identity.AccidentYear)).GetValues())
        .AggregateDoubleArray();
}

public interface IProjectionConfiguration : IScope<(ImportIdentity Id, string AmountType, string EstimateType, 
    int? AccidentYear, int ReportingPeriodMonth, int ReportingPeriodYear), ImportStorage>
{
    ProjectionConfiguration[] GetProjectionConfigurations() => GetStorage()
        .GetProjectionConfigurations().SortRelevantProjections(Identity.ReportingPeriodMonth);
    // TODO: make sure that the argument of SortRelevntProjection is correct (A.K., 15.02.24)
    public int FirstNextYearProjection() => GetProjectionConfigurations()
        .TakeWhile(x => x.Shift < Identity.ReportingPeriodMonth).Count() +
            Identity.ReportingPeriodMonth == Consts.MonthInAYear ? -1 :0;
}