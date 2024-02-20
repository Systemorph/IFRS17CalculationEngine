using System.ComponentModel.DataAnnotations;
using OpenSmc.Domain.Abstractions;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

public record ReportingNode : KeyedDimension, IHierarchicalDimension
{
    [Dimension(typeof(ReportingNode))] public string? Parent { get; init; }

    [Required]
    [Dimension(typeof(Currency))]
    public virtual string? Currency { get; init; }
}