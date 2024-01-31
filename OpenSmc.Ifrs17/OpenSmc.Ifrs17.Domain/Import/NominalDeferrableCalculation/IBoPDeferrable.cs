using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Import.NominalCashflow;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.NominalDeferrableCalculation;

public interface IBoPDeferrable : INominalDeferrable
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<INominalDeferrable>(s => s.WithApplicability<INominalDeferrableFromIfrsVariable>(x => x.Identity.Id.Novelty == Novelties.I));
    private int ProjectionShift => GetStorage().GetShift(Identity.Id.ProjectionPeriod);
    double INominalDeferrable.Value => GetScope<INominalCashflow>((Identity.Id, AmountTypes.DAE, EstimateTypes.BE, (int?)null)).Values //loop over AM under DE
        .Skip(ProjectionShift + Identity.MonthlyShift).FirstOrDefault();
}