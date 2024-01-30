using System.ComponentModel.DataAnnotations;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.DataModel.Interfaces;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Partition;


namespace OpenSmc.Ifrs17.Domain.DataModel;

public record DataNode : KeyedDimension, IPartitioned
{
    [NotVisible]
    [PartitionKey(typeof(PartitionByReportingNode))]
    public Guid Partition { get; init; }

    [NotVisible]
    [Dimension(typeof(Currency))]
    public string ContractualCurrency { get; init; }

    [NotVisible]
    [Dimension(typeof(Currency))]
    public string FunctionalCurrency { get; init; }

    [NotVisible]
    [Dimension(typeof(LineOfBusiness))]
    public string LineOfBusiness { get; init; }

    [NotVisible]
    [Dimension(typeof(ValuationApproach))]
    [Required]
    public string ValuationApproach { get; init; }

    [NotVisible]
    [Dimension(typeof(OciType))]
    public string OciType { get; init; }
}