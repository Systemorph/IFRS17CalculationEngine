using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import.NominalCashflow;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Arithmetics;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IDiscountedCashflow : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IDiscountedCashflow>(s => s
            .WithApplicability<IDiscountedCashflowNextYearsProjection>(x => x.Identity.Id.ProjectionPeriod > x.GetStorage().FirstNextYearProjection)
            .WithApplicability<IDiscountedCreditRiskCashflow>(x => x.Identity.Id.IsReinsurance && x.GetStorage().GetCdr().Contains(x.Identity.AmountType)));

    private PeriodType PeriodType => GetStorage().GetPeriodType(Identity.AmountType, Identity.EstimateType); 

    private string EconomicBasis => GetContext();
    protected double[] MonthlyDiscounting => GetScope<IMonthlyRate>(Identity.Id, o => o.WithContext(EconomicBasis)).Discount;
    protected double[] NominalValues => GetScope<INominalCashflow>(Identity).Values;

    double[] Values => ArithmeticOperations.Multiply(-1d, NominalValues.ComputeDiscountAndCumulate(MonthlyDiscounting, PeriodType)); // we need to flip the sign to create a reserve view
}