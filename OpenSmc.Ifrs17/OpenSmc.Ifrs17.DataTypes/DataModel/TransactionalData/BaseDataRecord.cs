using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.Interfaces;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;

public abstract record BaseDataRecord : BaseVariableIdentity, IKeyed
{
    [Key][NotVisible] public Guid Id { get; init; }

    //[NotVisible]
    //[PartitionKey(typeof(PartitionByReportingNodeAndPeriod))]
    //public Guid Partition { get; init; }

    //[Conversion(typeof(PrimitiveArrayConverter))]
    public double[] Values { get; init; }

    public double Value { get; set; }

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

    [NotVisible]
    [Dimension(typeof(Scenario))]
    [IdentityProperty]
    public string? Scenario { get; init; }

    [NotVisible]
    [Dimension(typeof(int), nameof(Year))]
    [IdentityProperty]
    public int Year { get; init;}

    [NotVisible]
    [Dimension(typeof(int), nameof(Month))]
    [IdentityProperty]
    public int Month { get; init; }

    [NotVisible]
    [Dimension(typeof(ReportingNode))]
    [IdentityProperty]
    public string ReportingNode { get; init; }
}