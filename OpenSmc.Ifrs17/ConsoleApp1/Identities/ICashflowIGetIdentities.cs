using OpenSmc.Collections;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSms.Ifrs17.CalculationScopes;
using OpenSms.Ifrs17.CalculationScopes.AocSteps;

namespace OpenSms.Ifrs17.CalculationScopes.Identities;

public interface ICashflowIGetIdentities : IGetIdentities
{
    private bool IsReinsurance => GetStorage().DataNodeDataBySystemName[Identity].IsReinsurance;

    private IEnumerable<ImportIdentity> ParsedIdentities => GetScope<IParsedAocSteps>(Identity)
        .Values
        .Select(aocStep => new ImportIdentity { AocType = aocStep.AocType, Novelty = aocStep.Novelty, DataNode = Identity });

    private IEnumerable<string> RawVariableNovelties => GetStorage().GetRawVariables(Identity).Select(rv => rv.Novelty).Concat(Novelties.C.RepeatOnce()).ToHashSet();

    private IEnumerable<AocStep> calculatedAocSteps => GetStorage().AocConfigurationByAocStep
        .Values
        .Where(x => ImportCalculationExtensions.ComputationHelper
                        .CurrentPeriodCalculatedDataTypes
                        .Any(y => x.DataType.RepeatOnce().Contains(y)) &&
                            (!IsReinsurance ? !ImportCalculationExtensions
                                .ComputationHelper.ReinsuranceAocType
                                .Contains(x.AocType) : true) && 
                            RawVariableNovelties.Contains(x.Novelty) ||
                            x.DataType.RepeatOnce()
                                .Contains(DataType.CalculatedProjection))
        .Select(x => new AocStep(x.AocType, x.Novelty));

    private IEnumerable<ImportIdentity> SpecialIdentities => calculatedAocSteps
        .Select(x => new ImportIdentity { AocType = x.AocType, Novelty = x.Novelty, DataNode = Identity })
        .Concat(GetStorage().AocConfigurationByAocStep
            .Values
            .Where(x => (!IsReinsurance ? !ImportCalculationExtensions.ComputationHelper
                .ReinsuranceAocType.Contains(x.AocType) : true) && 
                        x.DataType.RepeatOnce().Contains(DataType.Calculated) && x.Novelty == Novelties.I)
            .Select(aocStep => new ImportIdentity { AocType = aocStep.AocType, Novelty = aocStep.Novelty, DataNode = Identity }));

    IEnumerable<ImportIdentity> IGetIdentities.allIdentities => ParsedIdentities.Concat(SpecialIdentities).Distinct();
}