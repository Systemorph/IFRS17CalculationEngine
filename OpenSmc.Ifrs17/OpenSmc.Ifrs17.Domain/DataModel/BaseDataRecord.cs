using System.ComponentModel.DataAnnotations;
using OpenSmc.Ifrs17.Domain.DataModel.Interfaces;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Partition;
using Systemorph.Vertex.Persistence.EntityFramework.Conversions.Api;
using Systemorph.Vertex.Persistence.EntityFramework.Conversions.Converters;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public abstract record BaseDataRecord : BaseVariableIdentity, IKeyed, IPartitioned
{
    [Key][NotVisible] public Guid Id { get; init; }

    [NotVisible]
    [PartitionKey(typeof(PartitionByReportingNodeAndPeriod))]
    public Guid Partition { get; init; }

    [Conversion(typeof(PrimitiveArrayConverter))]
    public double[] Values { get; set; }

    [NotVisible]
    [Dimension(typeof(EstimateType))]
    [IdentityProperty]
    public string EstimateType { get; init; }

    [NotVisible]
    [Dimension(typeof(AmountType))]
    [IdentityProperty]
    public string? AmountType { get; init; }

    [NotVisible]
    [Dimension(typeof(int), nameof(AccidentYear))]
    [IdentityProperty]
    public int? AccidentYear { get; init; }
}