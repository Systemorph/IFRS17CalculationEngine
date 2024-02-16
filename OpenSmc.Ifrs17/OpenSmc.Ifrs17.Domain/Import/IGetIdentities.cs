using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IGetIdentities : IScope<string, ImportStorageOld>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IGetIdentities>(s => s.WithApplicability<ICashflowIGetIdentities>(x => x.GetStorage().ImportFormat == ImportFormats.Cashflow));
    
    protected IEnumerable<ImportIdentity> allIdentities => GetStorage().AocConfigurationByAocStep.Values.Select(x => new ImportIdentity {AocType = x.AocType, Novelty = x.Novelty, DataNode = Identity });

    IEnumerable<ImportIdentity> Identities => allIdentities.Select(id => id with { IsReinsurance = GetStorage().DataNodeDataBySystemName[id.DataNode].IsReinsurance,
        ValuationApproach = GetStorage().DataNodeDataBySystemName[id.DataNode].ValuationApproach,
        LiabilityType = GetStorage().DataNodeDataBySystemName[id.DataNode].LiabilityType });
    IEnumerable<AocStep> AocSteps => Identities.Select(id => id.AocStep).Distinct();
}