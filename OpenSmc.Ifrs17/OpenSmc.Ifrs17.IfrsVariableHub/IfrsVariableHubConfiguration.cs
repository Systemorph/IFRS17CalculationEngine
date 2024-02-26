using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.IfrsVariableHub;

/* For now, this hub owns all data except Dimensions and ReportVariable (subject to change).
 */
public static class IfrsVariableHubConfiguration
{
    public static MessageHubConfiguration ConfigureIfrsDataDictInit(this MessageHubConfiguration configuration) =>
        configuration.AddData(dc => dc.WithDataSource("IfrsVariableData", 
            ds => ds.WithType<IfrsVariable>(t => t.
                WithKey(iv => (iv.ReportingNode, iv.Year, iv.Month, iv.Scenario,
                    iv.EconomicBasis, iv.EstimateType, iv.AmountType, iv.AccidentYear,  
                    iv.DataNode, iv.AocType, iv.Novelty))
                .WithInitialData(TemplateData.SimpleValueReferenceData))));
}




