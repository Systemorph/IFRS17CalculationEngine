using OpenSmc.Equality;
using System.Linq.Expressions;

namespace OpenSmc.Ifrs17.Utils;

public class EqualityComparer<T> : IEqualityComparer<T>
{
    private static readonly System.Reflection.PropertyInfo[] IdentityProperties = typeof(T).GetIdentityProperties().ToArray();
    private static Func<T, T, bool> compiledEqualityFunction;

    private EqualityComparer()
    {
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
