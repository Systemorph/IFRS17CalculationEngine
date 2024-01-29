using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IBeAmountTypesFromIfrsVariables : IValidAmountType
{
    IEnumerable<string> IValidAmountType.BeAmountTypes => GetStorage().GetIfrsVariables(Identity)
        .Where(iv => GetStorage().EstimateTypesByImportFormat[ImportFormats.Cashflow].Contains(iv.EstimateType) && iv.AmountType != null)
        .Select(x => x.AmountType).ToHashSet();
}