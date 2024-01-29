using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import.TechnicalMarginAllocation;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface ILossComponent : IScope<ImportIdentity, ImportStorage>
{
    [NotVisible]string EstimateType => EstimateTypes.L;
    
    double Value => GetScope<IAllocateTechnicalMargin>(Identity, o => o.WithContext(EstimateType)).Value;
}