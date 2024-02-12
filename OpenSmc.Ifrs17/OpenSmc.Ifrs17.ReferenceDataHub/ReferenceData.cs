using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;

namespace OpenSmc.Ifrs17.ReferenceDataHub
{
    public class ReferenceData
    {
        public AocStep[] ReferenceAocSteps { get; set; }

        public AmountType[] ReferenceAmountTypes { get; set; }

        public ReferenceData()
        {
            ReferenceAocSteps = new AocStep[]
            {
                new("BoP", "I"), new("IA", "I"), new("NB", "N"), new("C", "M")
            };

            ReferenceAmountTypes =
            [
                new AmountType() {SystemName = "E", DisplayName = "Expenses"},
                new AmountType() {SystemName = "P", DisplayName = "Premiums"}
            ];
        }
    };
}
