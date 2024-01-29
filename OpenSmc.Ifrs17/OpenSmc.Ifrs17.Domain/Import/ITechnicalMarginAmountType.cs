using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface ITechnicalMarginAmountType : IScope<(ImportIdentity Id, string EstimateType), ImportStorage>
{
    protected IEnumerable<string> AmountTypesToExclude => (Identity.EstimateType, Identity.Id.ValuationApproach) switch {
        (EstimateTypes.LR, ValuationApproaches.PAA) => GetStorage().GetCoverageUnits().Concat(GetStorage().GetNonAttributableAmountType()).Concat(AmountTypes.CDR.RepeatOnce()).Concat(GetStorage().GetAttributableExpenses()).Concat(GetStorage().GetDeferrableExpenses()).Concat(GetStorage().GetPremiums()),
        (EstimateTypes.LR, _) => GetStorage().GetCoverageUnits().Concat(GetStorage().GetNonAttributableAmountType()).Concat(AmountTypes.CDR.RepeatOnce()).Concat(GetStorage().GetAttributableExpenses()),
        (_, ValuationApproaches.PAA) => GetStorage().GetCoverageUnits().Concat(GetStorage().GetNonAttributableAmountType()).Concat(AmountTypes.CDR.RepeatOnce()).Concat(GetStorage().GetDeferrableExpenses()).Concat(GetStorage().GetPremiums()),
        (_) => GetStorage().GetCoverageUnits().Concat(GetStorage().GetNonAttributableAmountType()).Concat(AmountTypes.CDR.RepeatOnce())
    };

    IEnumerable<string> Values => GetScope<IValidAmountType>(Identity.Id.DataNode).BeAmountTypes.Except(AmountTypesToExclude);
}