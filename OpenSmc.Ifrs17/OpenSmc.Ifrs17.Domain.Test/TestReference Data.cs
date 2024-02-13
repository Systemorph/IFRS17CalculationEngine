using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;

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
                new AmountType() {SystemName = "E", DisplayName = "Expenses"},
                new AmountType() {SystemName = "P", DisplayName = "Premiums"}
            ];
        }
    };
}
