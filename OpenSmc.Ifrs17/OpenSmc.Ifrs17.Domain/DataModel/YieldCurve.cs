using System.ComponentModel.DataAnnotations;
using OpenSmc.Arithmetics;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.Domain.DataModel.Interfaces;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Persistence.EntityFramework.Conversions.Api;
using Systemorph.Vertex.Persistence.EntityFramework.Conversions.Converters;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record YieldCurve : KeyedRecord, IWithYearMonthAndScenario
{
    [Required]
    [IdentityProperty]
    [Dimension(typeof(Currency))]
    public string? Currency { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Year))]
    [Range(1900, 2100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Year { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Month))]
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Month { get; init; }

    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string? Scenario { get; init; }

    [IdentityProperty] public string? Name { get; init; }

    [Conversion(typeof(PrimitiveArrayConverter))]
    public double[] Values { get; init; }
}