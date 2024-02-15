namespace OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;

public record RawVariable : BaseDataRecord
{
    public string CashFlowPeriodicity { get; set; }

    public string InterpolationMethod { get; set; }
}