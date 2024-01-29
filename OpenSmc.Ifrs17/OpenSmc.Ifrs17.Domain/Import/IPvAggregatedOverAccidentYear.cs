using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IPvAggregatedOverAccidentYear : IScope<(ImportIdentity Id, string AmountType, string EstimateType), ImportStorage>
{   
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))]
    string EconomicBasis => GetContext();
        
    private int?[] AccidentYears => GetStorage().GetAccidentYears(Identity.Id.DataNode, Identity.Id.ProjectionPeriod).ToArray();  
    
    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] PresentValues => AccidentYears.Select(ay => 
            (Identity.AmountType, Identity.EstimateType, ay, GetScope<IPresentValue>((Identity.Id, Identity.AmountType, Identity.EstimateType, ay), o => o.WithContext(EconomicBasis)).Value))
        .ToArray();
            
    double Value => PresentValues.Sum(pv => pv.Value);
}