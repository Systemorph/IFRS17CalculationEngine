using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import.TechnicalMarginAllocation;
using OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IContractualServiceMargin : IScope<ImportIdentity, ImportStorageOld>
{
    [NotVisible]string EstimateType => EstimateTypes.C;
     
    double Value => Identity.IsReinsurance 
        ? -1d * GetScope<ITechnicalMargin>(Identity, o => o.WithContext(EstimateType)).Value
        : -1d * GetScope<IAllocateTechnicalMargin>(Identity, o => o.WithContext(EstimateType)).Value;
}