using FluentAssertions;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;

namespace OpenSmc.Ifrs17.Domain.Test;

public class EqualityComparerTest
{
    [Fact]
    public void RunTests()
    {
        var comparer = YieldCurveComparer.Instance();


        var yc = new YieldCurve()
        {
            Currency = "EUR",
            Year = 2000,
            Month = 1,
            Name = default,
            Scenario = default,
            Values = new double[] {0.001, 0.002, 0.003, 0.004}
        };


        comparer.Equals(yc, yc).Should().BeTrue();


        comparer.Equals(yc, null).Should().BeFalse();


        comparer.Equals(yc, yc with
        {
            Month = 19
        }).Should().BeTrue();


        comparer.Equals(yc, yc with
        {
            Year = 2001
        }).Should().BeFalse();


        comparer.Equals(yc, yc with
        {
            Year = 2001, Values = new[] {0.002, 0.003, 0.004}
        }).Should().BeTrue();


        comparer.Equals(yc, yc with
        {
            Year = 1999
        }).Should().BeFalse();


        comparer.Equals(yc, yc with
        {
            Year = 1999, Values = new[] {0.001, 0.001, 0.002, 0.003, 0.004}
        }).Should().BeTrue();


        comparer.Equals(yc, yc with
        {
            Values = new[] {0.001, 0.001, 0.002, 0.003, 0.004}
        }).Should().BeFalse();


        var ifrsComparer = IfrsVariableComparer.Instance();


        var iv1 = new IfrsVariable()
        {
            AmountType = "CL",
            AccidentYear = 2021,
            Novelty = "N",
            DataNode = "GR1",
            AocType = "EOP",
            EstimateType = "PL",
            Values = new double[] {67.5, 57.0, 33.44, 30.12, 12.1, 0.0d}
        };

        var iv2 = new IfrsVariable()
        {
            AmountType = "CL",
            AccidentYear = 2021,
            Novelty = "N",
            DataNode = "GR1",
            AocType = "EOP",
            EstimateType = "PL",
            Values = new double[] {67.5, 57.0, 33.44, 30.12, 12.1, 5.03}
        };


        ifrsComparer.Equals(iv1, iv1).Should().BeTrue();


        ifrsComparer.Equals(iv1, iv2).Should().BeFalse();


        ifrsComparer.Equals(iv1, iv1 with
        {
            Values = iv1.Values.Concat(new double[] {0, 0.1}).ToArray()
        }).Should().BeFalse();
    }
}


