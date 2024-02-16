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
                new AmountType("E", "Expenses", "", 10, PeriodType.BeginningOfPeriod),
                new AmountType("PR", "Premiums", "", 10, PeriodType.BeginningOfPeriod)
            ];
        }
    };
}
