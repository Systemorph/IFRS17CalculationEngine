using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IAllocateTechnicalMarginWithIfrsVariable: IScope<ImportIdentity, ImportStorage>
{                                  
    double Value => ComputeTechnicalMarginFromIfrsVariables(Identity);
    double AggregatedValue => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
        .Sum(aoc => ComputeTechnicalMarginFromIfrsVariables(Identity with {AocType = aoc.AocType, Novelty = aoc.Novelty}));
                                                                    
    private double ComputeTechnicalMarginFromIfrsVariables(ImportIdentity id) =>
        GetStorage().GetValue(Identity, null, EstimateTypes.L, null, Identity.ProjectionPeriod) - 
        GetStorage().GetValue(Identity, null, EstimateTypes.C, null, Identity.ProjectionPeriod);
}