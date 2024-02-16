using OpenSmc.Arithmetics;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface ITelescopicDifference : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? Accidentyear), ImportStorageOld>
{
    [NotVisible]
    string EconomicBasis => GetContext();
    private double[] CurrentValues => GetScope<IDiscountedCashflow>(Identity).Values;
    
    private double[] PreviousValues => (GetScope<IParentAocStep>((Identity.Id, Identity.AmountType, StructureType.AocPresentValue)))
        .Values
        .Select(aoc => GetScope<IDiscountedCashflow>((Identity.Id with {AocType = aoc.AocType, Novelty = aoc.Novelty}, Identity.AmountType, Identity.EstimateType, Identity.Accidentyear)).Values)
        .Where(cf => cf.Count() > 0)
        .AggregateDoubleArray();
    
    double[] Values => ArithmeticOperations.Subtract(CurrentValues, PreviousValues);
}