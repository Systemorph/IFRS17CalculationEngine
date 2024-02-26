using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;

namespace OpenSmc.Ifrs17.Domain.Test
{
    public class TestReferenceData
    {
        public AocStep[] ReferenceAocSteps { get; set; }

        public AmountType[] ReferenceAmountTypes { get; set; }

        public TestReferenceData()
        {
            Reset();
        }

        public void Reset()
        {
            ReferenceAocSteps =
            [
                new("BoP", "I"), new("IA", "I"), new("NB", "N"), new("C", "M")
            ];

            ReferenceAmountTypes =
            [
                new AmountType { SystemName = "E", DisplayName = "Expenses", Parent = "", Order = 10, PeriodType = PeriodType.BeginningOfPeriod },
                new AmountType { SystemName = "PR", DisplayName = "Premiums", Parent = "", Order = 10, PeriodType = PeriodType.BeginningOfPeriod }
            ];
        }
    };
}
