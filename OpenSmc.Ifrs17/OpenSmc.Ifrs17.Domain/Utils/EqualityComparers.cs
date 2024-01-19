#!import "../DataModel/DataStructure"
using System;


public static bool SequenceEqual(this double[] defaultArray, double[] testArray, double precision)
{
    if ( defaultArray == null || testArray == null ) return false; 
    if (defaultArray.Length != testArray.Length) return false;
    for (int i = 0; i < defaultArray.Length; i++){
        if(Math.Abs(defaultArray[i]-testArray[i]) >= precision) return false;
    }
    return true;
}


class RawVariableComparer: IEqualityComparer<RawVariable>
{
    private bool IgnoreValues;
    private RawVariableComparer(bool ignoreValues)
    {
        IgnoreValues = ignoreValues;
    }

    public bool Equals(RawVariable x, RawVariable y) =>
        x.AccidentYear == y.AccidentYear && x.AmountType == y.AmountType && x.DataNode == y.DataNode && x.AocType == y.AocType && 
        x.Novelty == y.Novelty && x.EstimateType == y.EstimateType && (IgnoreValues ? true : x.Values.SequenceEqual(y.Values, Precision));

    public int GetHashCode(RawVariable v) => 0;

    public static RawVariableComparer Instance(bool ignoreValues = false) => new RawVariableComparer(ignoreValues);
}


class IfrsVariableComparer: IEqualityComparer<IfrsVariable>
{
    private bool IgnoreValues;
    private double precision;
    private IfrsVariableComparer(bool ignoreValues, double precision)
    {
        IgnoreValues = ignoreValues;
        this.precision = precision;
    }

    private bool CompareValues(double[] value1, double[] value2){
        if((value1 == null && value2 != null) || 
            (value1 != null && value2 == null) || 
            (value1.Count() != value2.Count())) return false;
        if(value1 == null && value2 == null) return true;
        return value1.Select((x, i) => Math.Abs(x - value2.ElementAt(i))).All(x => x < precision);
    }

    public bool Equals(IfrsVariable x, IfrsVariable y) =>
        x.AccidentYear == y.AccidentYear && 
        x.AmountType == y.AmountType && 
        x.DataNode == y.DataNode && 
        x.AocType == y.AocType && 
        x.Novelty == y.Novelty && 
        x.EstimateType == y.EstimateType && 
        x.EconomicBasis == y.EconomicBasis && 
        (IgnoreValues ? true : CompareValues(x.Values, y.Values)); 

    public int GetHashCode(IfrsVariable v) => 0;

    public static IfrsVariableComparer Instance(bool ignoreValues = false, double precision = Precision) => new IfrsVariableComparer(ignoreValues, precision);
}


class YieldCurveComparer: IEqualityComparer<YieldCurve>
{
    private YieldCurveComparer(){}

    public bool Equals(YieldCurve x, YieldCurve y)
    {
        if (x == null && y == null)
            return true; 
        if (x == null || y == null)
            return false; 
        if (!(x.Scenario == y.Scenario && x.Currency == y.Currency && x.Name == y.Name))
            return false; 
        if (x.Year == y.Year)
            return x.Values.SequenceEqual(y.Values, YieldCurvePrecision); 
        return x.Year > y.Year
            ? x.Values.SequenceEqual(y.Values.Skip(x.Year - y.Year).ToArray(), YieldCurvePrecision)
            : x.Values.Skip(y.Year - x.Year).ToArray().SequenceEqual(y.Values, YieldCurvePrecision);
    }
	
    public int GetHashCode (YieldCurve x) => 0;

    public static YieldCurveComparer Instance() => new YieldCurveComparer();
}


class ParametersComparer: IEqualityComparer<DataNodeParameter>
{
    private ParametersComparer(){}

    public bool Equals(DataNodeParameter x, DataNodeParameter y) {
        if (x == null && y == null) return true; 
        if (x == null || y == null) return false; 
        if (!(x.Year == y.Year && x.Month == y.Month && x.Scenario == y.Scenario)) return false; 
        if (x is SingleDataNodeParameter && y is SingleDataNodeParameter && x.DataNode == y.DataNode &&
            ((SingleDataNodeParameter)x).PremiumAllocation == ((SingleDataNodeParameter)y).PremiumAllocation) return true; 
        if (x is InterDataNodeParameter && y is InterDataNodeParameter) {
            var xi = (InterDataNodeParameter)x;
            var yi = (InterDataNodeParameter)y;
            if (xi.ReinsuranceCoverage != yi.ReinsuranceCoverage) return false;
            if ((xi.LinkedDataNode == yi.LinkedDataNode && xi.DataNode == yi.DataNode) ||
                (xi.LinkedDataNode == yi.DataNode && xi.DataNode == yi.LinkedDataNode)) return true;
        }
        return false;
    }
	
    public int GetHashCode (DataNodeParameter x) => 0;

    public static ParametersComparer Instance() => new ParametersComparer();
}


using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
//using Systemorph.Domain;
//using Systemorph.Utils.Reflection;
using static Systemorph.Vertex.Equality.IdentityPropertyExtensions;


class EqualityComparer<T> : IEqualityComparer<T>
{
    private static readonly System.Reflection.PropertyInfo[] IdentityProperties = typeof(T).GetIdentityProperties().ToArray();
    private static Func<T, T, bool> compiledEqualityFunction;

    private EqualityComparer() {
        compiledEqualityFunction = GetEqualityFunction();
    }

    public static readonly EqualityComparer<T> Instance = new EqualityComparer<T>();

    public bool Equals(T x, T y) => compiledEqualityFunction(x, y);
    public int GetHashCode(T obj) => 0;

    private static Func<T, T, bool> GetEqualityFunction()
    {
        var prm1 = Expression.Parameter(typeof(T));
        var prm2 = Expression.Parameter(typeof(T));

        // r1 == null && r2 == null
        var nullConditionExpression = Expression.AndAlso(Expression.Equal(prm1, Expression.Constant(null, typeof(T))), Expression.Equal(prm2, Expression.Constant(null, typeof(T))));
        // r1 != null && r2 != null
        var nonNullConditionExpression = Expression.AndAlso(Expression.NotEqual(prm1, Expression.Constant(null, typeof(T))), Expression.NotEqual(prm2, Expression.Constant(null, typeof(T))));
        // r1.prop1 == r2.prop1 && r1.prop2 == r2.prop2...... 
        var allPropertiesEqualExpression = IdentityProperties.Select(propertyInfo => Expression.Equal(Expression.Property(prm1, propertyInfo), Expression.Property(prm2, propertyInfo))).Aggregate(Expression.AndAlso);

        var equalityExpr = Expression.OrElse(nullConditionExpression, Expression.AndAlso(nonNullConditionExpression, allPropertiesEqualExpression));
        return Expression.Lambda<Func<T, T, bool>>(equalityExpr, prm1, prm2).Compile();
    }
}
