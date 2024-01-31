using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface ILoReCoBoundary : IScope<ImportIdentity, ImportStorage>
{
    private IEnumerable<string> UnderlyingGic => GetStorage().GetUnderlyingGic(Identity, LiabilityTypes.LRC);
   
    double Value => UnderlyingGic.Sum(gic => GetStorage().GetReinsuranceCoverage(Identity, gic) * GetScope<ILossComponent>(GetStorage().GetUnderlyingIdentity(Identity, gic)).Value);
                                                                      
    double AggregatedValue => UnderlyingGic.Sum(gic => GetStorage().GetReinsuranceCoverage(Identity, gic) * 
                                                       GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
                                                           .Sum(aoc => GetScope<ILossComponent>(Identity with {DataNode = gic, AocType = aoc.AocType, Novelty = aoc.Novelty}).Value));
}