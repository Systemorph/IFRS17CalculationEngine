using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IValidAmountType : IScope<string, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IValidAmountType>(s => s.WithApplicability<IBeAmountTypesFromIfrsVariables>(x => x.GetStorage().ImportFormat != ImportFormats.Cashflow ||
            x.GetStorage().IsSecondaryScope(x.Identity)));
    
    IEnumerable<string> BeAmountTypes => GetStorage().GetRawVariables(Identity)
        .Where(rv => rv.AmountType != null).Select(x => x.AmountType).Concat(
            GetStorage().DataNodeDataBySystemName[Identity].IsReinsurance 
                ? GetStorage().DataNodeDataBySystemName[Identity].LiabilityType == LiabilityTypes.LIC 
                    ? new []{AmountTypes.CDR} : new []{AmountTypes.CDRI, AmountTypes.CDR} 
                : Enumerable.Empty<string>()).ToHashSet();
    
    IEnumerable<string> ActualAmountTypes => GetStorage().GetIfrsVariables(Identity)
        .Where(iv => GetStorage().EstimateTypesByImportFormat[ImportFormats.Actual].Contains(iv.EstimateType))
        .Select(x => x.AmountType).ToHashSet();

    IEnumerable<string> AllImportedAmountTypes => BeAmountTypes.Union(ActualAmountTypes).ToHashSet();
}