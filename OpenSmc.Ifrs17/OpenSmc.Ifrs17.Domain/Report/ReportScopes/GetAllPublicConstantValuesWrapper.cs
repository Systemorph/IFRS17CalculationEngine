using System.Reflection;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes;

public static class AllPublicConstantValuesWrapper
{
    public static T?[] GetAllPublicConstantValues<T>(this Type type,
        IList<T>? excludedTerms = null)
    {
        var selection = type
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(T))
            .Select(x => (T)x.GetRawConstantValue())
            .ToArray();
        if (excludedTerms == null)
            return selection;
        else
            return selection.Where(x => !excludedTerms.Contains(x)).ToArray();
    }
}

// public interface ExperienceAdjustmentOnAcquistionExpenses: IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable> {
//     static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
//         builder.ForScope<ExperienceAdjustmentOnAcquistionExpenses>(s => s.WithApplicability<ExperienceAdjustmentOnAcquistionExpensesNotApplicable>(x => x.Identity.Id.IsReinsurance || x.Identity.Id.LiabilityType == LiabilityTypes.LIC));

//     private IDataCube<ReportVariable> WrittenAcquistionExpenses => GetScope<IWrittenAndAccruals>(Identity).Written.Filter(("VariableType", "CF"))
//         .Where(x =>
//             GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.AEA) ||
//             GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.ACA)).ToDataCube();
//     private IDataCube<ReportVariable> BestEstimateAcquistionExpenses => GetScope<IBestEstimate>(Identity).IBestEstimate.Filter(("VariableType", "CF"))
//         .Where(x =>
//             GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.AEA) ||
//             GetStorage().GetHierarchy<AmountType>().Ancestors(x.AmountType, includeSelf: true).Any(x => x.SystemName == AmountTypes.ACA)).ToDataCube();

//     IDataCube<ReportVariable> ExperienceAdjustmentOnAcquistionExpenses => (WrittenAcquistionExpenses - BestEstimateAcquistionExpenses)
//         .AggregateOver(nameof(Novelty), nameof(VariableType))
//         .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR8" });

// }

// public interface ExperienceAdjustmentOnAcquistionExpensesNotApplicable: ExperienceAdjustmentOnAcquistionExpenses {
//     IDataCube<ReportVariable> ExperienceAdjustmentOnAcquistionExpenses.ExperienceAdjustmentOnAcquistionExpenses=> Enumerable.Empty<ReportVariable>().ToArray().ToDataCube();      
// }