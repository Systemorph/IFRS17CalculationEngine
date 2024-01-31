using OpenSmc.Arithmetics;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import.NominalCashflow;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IWithInterestAccretion : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    private double[] ParentDiscountedValues => ArithmeticOperations.Multiply(-1d, GetScope<IDiscountedCashflow>(Identity).Values);    
    private double[] ParentNominalValues => GetScope<INominalCashflow>(Identity).Values;
    private double[] MonthlyInterestFactor => GetScope<IMonthlyRate>(Identity.Id).Interest;
    
    double[] GetInterestAccretion() 
    {
        if(!MonthlyInterestFactor.Any())
            return Enumerable.Empty<double>().ToArray();
        var periodType = GetStorage().GetPeriodType(Identity.AmountType, Identity.EstimateType);
        var ret = new double[ParentDiscountedValues.Length];
        
        switch (periodType) {
            case PeriodType.BeginningOfPeriod :
                for (var i = 0; i < ParentDiscountedValues.Length; i++)
                    ret[i] = -1d * (ParentDiscountedValues[i] - ParentNominalValues[i]) * (MonthlyInterestFactor.GetValidElement(i/12) - 1d );
                break;
            default :
                for (var i = 0; i < ParentDiscountedValues.Length; i++)
                    ret[i] = -1d * ParentDiscountedValues[i] * (MonthlyInterestFactor.GetValidElement(i/12) - 1d );
                break;
        }
        
        return ret;
    }
}