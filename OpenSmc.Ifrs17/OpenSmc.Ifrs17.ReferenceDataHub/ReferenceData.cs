using System;
using System.Collections.Generic;
using System.Linq;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;

namespace OpenSmc.Ifrs17.ReferenceDataHub
{
    internal static class ReferenceData
    {
        public static AocStep[] ReferenceAocSteps = new AocStep[]
        {
            new("BoP", "I"), new("IA", "I"), new("NB", "N"), new("C", "M")
        };

        public static AmountType[] ReferenceAmountTypes = new AmountType[]
        {
            new AmountType() {SystemName = "E", DisplayName = "Expenses"},
            new AmountType() {SystemName = "P", DisplayName = "Profit"}
        };
    };
}
