using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

public record DeferrableAmountType : AmountType
{
    public DeferrableAmountType(string systemName, string displayName, string parent, int order, PeriodType periodType) : base(systemName, displayName, parent, order, periodType)
    {
    }
}