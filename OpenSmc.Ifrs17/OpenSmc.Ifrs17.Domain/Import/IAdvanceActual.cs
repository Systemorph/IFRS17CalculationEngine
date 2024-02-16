using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.Domain.Import.AccrualActual;
using OpenSmc.Scopes;


namespace OpenSmc.Ifrs17.Domain.Import;

public interface IAdvanceActual : IScope<ImportIdentity, ImportStorageOld>
{
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))]
    string EstimateType => EstimateTypes.AA;
    
    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] Actuals => 
        GetScope<IValidAmountType>(Identity.DataNode).ActualAmountTypes
            .SelectMany(amountType => GetStorage().GetAccidentYears(Identity.DataNode, Identity.ProjectionPeriod)
                .Select(accidentYear => (amountType, EstimateType, accidentYear, GetScope<IAccrualActual>((Identity, amountType, EstimateType, accidentYear)).Value) )).ToArray();
}