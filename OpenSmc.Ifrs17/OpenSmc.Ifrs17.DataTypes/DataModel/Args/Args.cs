using System.ComponentModel.DataAnnotations;
using OpenSmc.Arithmetics;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.Domain.DataModel.Args;

public record Args
{
    [Required]
    [IdentityProperty]
    [Dimension(typeof(ReportingNode))]
    public string ReportingNode { get; init; }

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
    public string Scenario { get; init; }

    [IdentityProperty] public Periodicity Periodicity { get; init; }

    public Args(string reportingNode, int year, int month, Periodicity periodicity, string scenario)
    {
        ReportingNode = reportingNode;
        Year = year;
        Month = month;
        Periodicity = periodicity;
        Scenario = scenario;
    }
}