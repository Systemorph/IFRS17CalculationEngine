using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface ICumulatedNominalBe : IScope<ImportIdentity, ImportStorage> {  
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))]
    string EconomicBasis => EconomicBases.N;
    
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))]
    string EstimateType => EstimateTypes.BE;
    
    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] PresentValues => GetScope<IValidAmountType>(Identity.DataNode).BeAmountTypes
        .SelectMany(at => GetScope<IPvAggregatedOverAccidentYear>((Identity, at, EstimateType), o => o.WithContext(EconomicBasis)).PresentValues
        ).ToArray();
}