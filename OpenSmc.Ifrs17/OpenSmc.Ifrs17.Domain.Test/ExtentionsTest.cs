using FluentAssertions;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Test;

public class ExtensionsTest
{
    //public void CheckAggregateDoubleArray(double[][] arrayOfDoubleArrays, double[] bmAggregatedArray)
    //{
    //    var aggregatedArray = arrayOfDoubleArrays.AggregateDoubleArray();
    //    aggregatedArray.Should().BeEquivalentTo(bmAggregatedArray);
    //}

    //[Fact]
    //public void Test1()
    //{
    //    var array = new double[][]
    //    {
    //        new[] {-100d, -50d}, new[] {-100d, -50d, -50d, -50d, 0d, 0d, 0d, 0d, 0d, 0d,},
    //        new[] {-100d, -50d, -50d, -50d, 0d, 0d, 0d, 0d, 0d, 0d,},
    //        new[] {-100d, -50d, -50d, -50d, 0d, 0d, 0d, 0d, 0d, 0d,}
    //    };

    //    CheckAggregateDoubleArray(array, new[]
    //    {
    //        -400d, -200d, -150d, -150d, 0d, 0d, 0d, 0d, 0d, 0d,
    //    });
    //}

    //[Fact]
    //public void Test2()
    //{
    //    var array = new double[][]
    //    {
    //        null, new[] {-100d, -50d}
    //    };
    //    CheckAggregateDoubleArray(array, new[]
    //    {
    //        -100d, -50d
    //    });
    //}

    //[Fact]
    //public void Test3()
    //{
    //    var array = new double[][]
    //    {
    //        new[] {-100d, -50d}, null
    //    };
    //    CheckAggregateDoubleArray(array, new[]
    //    {
    //        -100d, -50d
    //    });
    //}

    //[Fact]
    //public void Test4()
    //{
    //    var array = new double[][]
    //    {
    //        Enumerable.Empty<double>().ToArray(), new[] {-100d, -50d}
    //    };
    //    CheckAggregateDoubleArray(array, new[]
    //    {
    //        -100d, -50d
    //    });
    //}

    //[Fact]
    //public void Test5()
    //{
    //    var array = new double[][]
    //    {
    //        Enumerable.Empty<double>().ToArray(), Enumerable.Empty<double>().ToArray()
    //    };
    //    CheckAggregateDoubleArray(array, Enumerable.Empty<double>().ToArray());
    //}

    //[Fact]
    //public void Test6()
    //{
    //    var array = new double[][]
    //    {
    //        new[] {-100d, -50d}
    //    };
    //    CheckAggregateDoubleArray(array, new[]
    //    {
    //        -100d, -50d
    //    });
    //}

    //[Fact]
    //public void Test7()
    //{
    //    var array = new double[][]
    //    {
    //        Enumerable.Empty<double>().ToArray()
    //    };
    //    CheckAggregateDoubleArray(array, Enumerable.Empty<double>().ToArray());
    //}

    //[Fact]
    //public void Test8()
    //{
    //    var array = new double[][]
    //    {
    //        null
    //    };
    //    CheckAggregateDoubleArray(array, Enumerable.Empty<double>().ToArray());
    //}

    //[Fact]
    //public void Test9()
    //{
    //    var cashflow = new double[] {120, 180};
    //    var yearly = cashflow.Interpolate(CashFlowPeriodicity.Yearly, InterpolationMethod.Uniform);
    //    (yearly[0], yearly[11], yearly[12], yearly[23]).Should().Be((10, 10, 15, 15));


    //    var quarterly = cashflow.Interpolate(CashFlowPeriodicity.Quarterly, InterpolationMethod.Uniform);
    //    (quarterly[0], quarterly[3], quarterly[4], quarterly[6]).Should().Be((30, 30, 45, 45));


    //    var monthly = cashflow.Interpolate(CashFlowPeriodicity.Monthly, InterpolationMethod.Uniform);
    //    (monthly[0], monthly[1]).Should().Be((120, 180));


    //    yearly = cashflow.Interpolate(CashFlowPeriodicity.Yearly, InterpolationMethod.Start);
    //    (yearly[0], yearly[11], yearly[12], yearly[23]).Should().Be((120, 0, 180, 0));


    //    yearly = cashflow.Interpolate(CashFlowPeriodicity.Yearly, InterpolationMethod.NotApplicable);
    //    (yearly[0], yearly[11], yearly[12], yearly[23]).Should().Be((10, 10, 15, 15));
    //}


    //public void CheckNormalizedArray(IEnumerable<double> source, double[] benchmark)
    //{
    //    var res = source.Normalize();
    //    res.Should().BeEquivalentTo(benchmark);
    //}

    //[Fact]
    //public void Test10()
    //{
    //    var array = new double[] {1, 1, 1};
    //    var benchmark = new double[] {1d / 3d, 1d / 3d, 1d / 3d};
    //    CheckNormalizedArray(array, benchmark);


    //    array = new double[] {-1, -1, -1};
    //    benchmark = new double[] {1d / 3d, 1d / 3d, 1d / 3d};
    //    CheckNormalizedArray(array, benchmark);


    //    array = new double[] {-1, +1, -1, +1};
    //    benchmark = Enumerable.Empty<double>().ToArray();
    //    CheckNormalizedArray(array, benchmark);


    //    array = new double[] { };
    //    benchmark = Enumerable.Empty<double>().ToArray();
    //    CheckNormalizedArray(array, benchmark);
    //}

    //[Fact]
    //public void Test12()
    //{
    //    DataType.CalculatedTelescopic.Contains(DataType.Optional).Should().BeFalse();
    //    (DataType.Optional | DataType.Calculated).Contains(DataType.CalculatedTelescopic).Should().BeFalse();
    //    (DataType.Optional | DataType.Calculated).Contains(DataType.Optional).Should().BeTrue();


    //    var datatypes = new DataType[] {DataType.CalculatedTelescopic, DataType.Optional | DataType.Calculated};


    //    datatypes.ContainsEnum(DataType.Optional).Should().BeTrue();
    //    datatypes.ContainsEnum(DataType.Calculated).Should().BeTrue();
    //    datatypes.ContainsEnum(DataType.Mandatory).Should().BeFalse();
    //}
}