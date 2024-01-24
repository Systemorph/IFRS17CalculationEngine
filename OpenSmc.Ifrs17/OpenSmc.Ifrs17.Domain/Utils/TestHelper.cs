using FluentAssertions;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Collections;

using Systemorph.Vertex.Pivot.Reporting;
using Systemorph.Vertex.Pivot.Reporting.Builder;

namespace OpenSmc.Ifrs17.Domain.Utils;

public static class TestUtils
{
    public const double NumericalPrecisionEqualityChecker = 1.0E-10;

    public static void EqualityComparer<T>(T[] collection1, T[] collection2)
    {
        collection1.Length.Should().Be(collection2.Length);
        var type = typeof(T);
        var properties = type.GetProperties().Where(p => p.Name != "Id").ToArray();
        if (properties.Count() == 0)
        {
            bool isEqual = Enumerable.SequenceEqual(collection1, collection2);
            isEqual.Should().Be(true);
        }

        foreach (var item1 in collection1)
        {
            var item2 = collection2.Where(x =>
                properties.All(prop =>
                {
                    var propType = prop.PropertyType;
                    var val = prop.GetValue(item1);
                    var otherVal = prop.GetValue(x);
                    if (val == null && otherVal == null) return true;
                    else if ((val != null && otherVal == null) || (val == null && otherVal != null)) return false;
                    else return Convert.ChangeType(otherVal, propType).Equals(Convert.ChangeType(val, propType));
                })
            );
            item2.Count().Should().NotBe(0);
        }
    }





    static bool CheckEquality(this double[] arr1, double[] arr2)
    {
        if (arr1.Length != arr2.Length) return false;
        for (int i = 0; i < arr1.Length; i++)
        {
            double d1 = arr1[i];
            double d2 = arr2[i];
            if (Math.Abs(d1) < NumericalPrecisionEqualityChecker &&
                Math.Abs(d1) < NumericalPrecisionEqualityChecker)
                continue;
            if (Math.Abs((d1 - d2) / d1) > NumericalPrecisionEqualityChecker)
                return false;
        }

        return true;
    }

    static bool CheckEquality(this IEnumerable<double> arr1, double[] arr2) => CheckEquality(arr1.ToArray(), arr2);
    static bool CheckEquality(this double[] arr1, IEnumerable<double> arr2) => CheckEquality(arr1, arr2.ToArray());

    static bool CheckEquality(this IEnumerable<double> arr1, IEnumerable<double> arr2) =>
        CheckEquality(arr1.ToArray(), arr2.ToArray());

    static bool CheckEquality(this double d1, double d2) => CheckEquality(d1.RepeatOnce(), d2.RepeatOnce());


    static bool CheckEquality(this double? d1, double? d2)
    {
        if (d1 == null && d2 == null) return true;
        else return CheckEquality((double) d1, (double) d2);
    }





    public static ReportBuilder<IfrsVariable, IfrsVariable, IfrsVariable>
        WithGridOptionsForIfrsVariable
        (this Systemorph.Vertex.Pivot.Builder.PivotBuilder<IfrsVariable, IfrsVariable, IfrsVariable> reportBuilder,
            int reportHeight = 650)
    {
        return reportBuilder.ToTable().WithOptions(go =>
            go.WithColumns(cols => cols.Modify("Value", c => c.WithWidth(300)
                        .WithFormat(
                            "new Intl.NumberFormat('en',{ minimumFractionDigits:2, maximumFractionDigits:2 }).format(value)")))
                    .WithRows(rows => rows.Where(r => !r.RowGroup.SystemName.EndsWith("NullGroup")).ToList())
                    .WithAutoGroupColumn(c => c.WithWidth(250) with {Pinned = "left"}) with
                {
                    Height = reportHeight, GroupDefaultExpanded = 2, OnGridReady = null
                }
        );
    }
}
