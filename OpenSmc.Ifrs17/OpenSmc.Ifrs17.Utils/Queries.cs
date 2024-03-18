using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.Interfaces;

namespace OpenSmc.Ifrs17.Utils;

public static class Queries
{
    public static IEnumerable<AocConfiguration> LoadAocStepConfiguration(this IWorkspace workspace, int year, int month)
        => workspace.LoadParameter<AocConfiguration>(year, month)
            .GroupBy(x => (x.AocType, x.Novelty),
            (k, v) => v.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).First());

    public static T[] LoadParameter<T>(this IWorkspace workspace, int year, int month, Func<T, bool>? filterExpression = null)
        where T : class, IWithYearAndMonth
    {
        return workspace.GetData<T>()
            .Where(x => x.Year == year && x.Month <= month || x.Year < year)
            .Where(filterExpression ?? (x => true))
            .ToArray();
    }
}
