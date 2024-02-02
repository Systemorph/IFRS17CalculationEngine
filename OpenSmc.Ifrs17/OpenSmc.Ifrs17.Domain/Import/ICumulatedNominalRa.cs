using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface ICumulatedNominalRa : IScope<ImportIdentity, ImportStorage> {  
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))]
    string EconomicBasis => EconomicBases.N;
    
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))]
    string EstimateType => EstimateTypes.RA;
    
    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] PresentValues => GetScope<IPvAggregatedOverAccidentYear>((Identity, (string)null, EstimateType), o => o.WithContext(EconomicBasis)).PresentValues;
}