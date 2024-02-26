using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;

namespace OpenSmc.Ifrs17.IfrsVariableHub;

public static class TemplateData
{
    public static IfrsVariable[] SimpleValueReferenceData = new[]
    {
        new IfrsVariable(){AmountType = "PR", AocType = "BOP", DataNode="DT5.1", EconomicBasis = "L", EstimateType="BE", Novelty="N", Values = [-798.5367312] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "NIC", AocType = "BOP", DataNode="DT5.1", EconomicBasis = "L", EstimateType="BE", Novelty="N", Values = [598.7529446] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "PR", AocType = "BOP", DataNode="DT5.1", EconomicBasis = "C", EstimateType="BE", Novelty="N", Values = [-798.5367312] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "NIC", AocType = "BOP", DataNode="DT5.1", EconomicBasis = "C", EstimateType="BE", Novelty="N", Values = [598.7529446] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "BOP", DataNode="DT5.1", EconomicBasis = "C", EstimateType="RA", Novelty="N", Values = [59.87529446] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "BOP", DataNode="DT5.1", EconomicBasis = "L", EstimateType="RA", Novelty="N", Values = [59.87529446] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "AM", DataNode="DT5.1", EconomicBasis = "L", EstimateType="F", Novelty="C", Values = [0.656229858] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "PR", AocType = "EOP", DataNode="DT5.1", EconomicBasis = "L", EstimateType="BE", Novelty="C", Values = [-399.6339295] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "NIC", AocType = "EOP", DataNode="DT5.1", EconomicBasis = "L", EstimateType="BE", Novelty="C", Values = [299.6755497] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "PR", AocType = "EOP", DataNode="DT5.1", EconomicBasis = "C", EstimateType="BE", Novelty="C", Values = [-399.6339295] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "NIC", AocType = "EOP", DataNode="DT5.1", EconomicBasis = "C", EstimateType="BE", Novelty="C", Values = [299.6755497] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "EOP", DataNode="DT5.1", EconomicBasis = "C", EstimateType="RA", Novelty="C", Values = [29.96755497] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "EOP", DataNode="DT5.1", EconomicBasis = "L", EstimateType="RA", Novelty="C", Values = [29.96755497] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "PR", AocType = "IA", DataNode="DT5.1", EconomicBasis = "L", EstimateType="BE", Novelty="N", Values = [-1.097198337] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "NIC", AocType = "IA", DataNode="DT5.1", EconomicBasis = "L", EstimateType="BE", Novelty="N", Values = [0.922605096] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "PR", AocType = "IA", DataNode="DT5.1", EconomicBasis = "C", EstimateType="BE", Novelty="N", Values = [-1.097198337] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "NIC", AocType = "IA", DataNode="DT5.1", EconomicBasis = "C", EstimateType="BE", Novelty="N", Values = [0.922605096] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "IA", DataNode="DT5.1", EconomicBasis = "C", EstimateType="RA", Novelty="N", Values = [0.09226051] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "IA", DataNode="DT5.1", EconomicBasis = "L", EstimateType="RA", Novelty="N", Values = [0.09226051] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "PR", AocType = "CF", DataNode="DT5.1", EconomicBasis = "L", EstimateType="BE", Novelty="N", Values = [400] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "NIC", AocType = "CF", DataNode="DT5.1", EconomicBasis = "L", EstimateType="BE", Novelty="N", Values = [-300] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "PR", AocType = "CF", DataNode="DT5.1", EconomicBasis = "C", EstimateType="BE", Novelty="N", Values = [400] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "NIC", AocType = "CF", DataNode="DT5.1", EconomicBasis = "C", EstimateType="BE", Novelty="N", Values = [-300] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "CF", DataNode="DT5.1", EconomicBasis = "C", EstimateType="RA", Novelty="N", Values = [-30] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "CF", DataNode="DT5.1", EconomicBasis = "L", EstimateType="RA", Novelty="N", Values = [-30] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "PR", AocType = "CF", DataNode="DT5.1", EconomicBasis = "L", EstimateType="BEPA", Novelty="N", Values = [320] ,Month = 12, ReportingNode ="CH", Year = 2020},   
        new IfrsVariable(){AmountType = "PR", AocType = "CF", DataNode="DT5.1", EconomicBasis = null, EstimateType="A", Novelty="C", Values = [400] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "NIC", AocType = "CF", DataNode="DT5.1", EconomicBasis = null, EstimateType="A", Novelty="C", Values = [-280] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "ACA", AocType = "CF", DataNode="DT5.1", EconomicBasis = null, EstimateType="A", Novelty="C", Values = [-10] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "AEA", AocType = "CF", DataNode="DT5.1", EconomicBasis = null, EstimateType="A", Novelty="C", Values = [-5] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = "PR", AocType = "CF", DataNode="DT5.1", EconomicBasis = null, EstimateType="APA", Novelty="C", Values = [320] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "BOP", DataNode="DT5.1", EconomicBasis = null, EstimateType="DA", Novelty="N", Values = [-15] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "EA", DataNode="DT5.1", EconomicBasis = null, EstimateType="C", Novelty="C", Values = [-15] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "AM", DataNode="DT5.1", EconomicBasis = null, EstimateType="DA", Novelty="C", Values = [9.85821304] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "AM", DataNode="DT5.1", EconomicBasis = null, EstimateType="C", Novelty="C", Values = [-82.02271122] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "EOP", DataNode="DT5.1", EconomicBasis = null, EstimateType="DA", Novelty="C", Values = [-5.16428696] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "EOP", DataNode="DT5.1", EconomicBasis = null, EstimateType="C", Novelty="C", Values = [42.96811361] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "BOP", DataNode="DT5.1", EconomicBasis = null, EstimateType="C", Novelty="N", Values = [139.9084921] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "IA", DataNode="DT5.1", EconomicBasis = null, EstimateType="C", Novelty="N", Values = [0.082332732] ,Month = 12, ReportingNode ="CH", Year = 2020},
        new IfrsVariable(){AmountType = null, AocType = "IA", DataNode="DT5.1", EconomicBasis = null, EstimateType="DA", Novelty="N", Values = [-0.0225] ,Month = 12, ReportingNode ="CH", Year = 2020},
    };
}