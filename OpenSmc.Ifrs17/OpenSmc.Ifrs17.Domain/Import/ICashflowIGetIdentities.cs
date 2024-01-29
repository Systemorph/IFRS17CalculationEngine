using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Collections;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface ICashflowIGetIdentities : IGetIdentities
{
    private bool isReinsurance => GetStorage().DataNodeDataBySystemName[Identity].IsReinsurance;
    private IEnumerable<ImportIdentity> ParsedIdentities => GetScope<IParsedAocSteps>(Identity).Values.Select(aocStep => new ImportIdentity {AocType = aocStep.AocType, Novelty = aocStep.Novelty, DataNode = Identity});
    private IEnumerable<string> rawVariableNovelties => GetStorage().GetRawVariables(Identity).Select(rv => rv.Novelty).Concat(Novelties.C.RepeatOnce()).ToHashSet();
    private IEnumerable<AocStep> calculatedAocSteps => GetStorage().AocConfigurationByAocStep.Values.Where(x => ImportCalculationExtensions.ComputationHelper.CurrentPeriodCalculatedDataTypes.Any(y => x.DataType.Contains(y)) &&
        (!isReinsurance ? !ImportCalculationExtensions.ComputationHelper.ReinsuranceAocType.Contains(x.AocType) : true) && rawVariableNovelties.Contains(x.Novelty) 
        || x.DataType.Contains(DataType.CalculatedProjection) ).Select(x => new AocStep(x.AocType, x.Novelty));
    private IEnumerable<ImportIdentity> specialIdentities => calculatedAocSteps.Select(x => new ImportIdentity {AocType = x.AocType, Novelty = x.Novelty, DataNode = Identity })
        .Concat(GetStorage().AocConfigurationByAocStep.Values.Where(x => (!isReinsurance ? !ImportCalculationExtensions.ComputationHelper.ReinsuranceAocType.Contains(x.AocType) : true) && x.DataType.Contains(DataType.Calculated) && x.Novelty == Novelties.I)
            .Select(aocStep => new ImportIdentity{AocType = aocStep.AocType, Novelty = aocStep.Novelty, DataNode = Identity}));

    IEnumerable<ImportIdentity> IGetIdentities.allIdentities => ParsedIdentities.Concat(specialIdentities).Distinct();       
}