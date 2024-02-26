using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.DataNodeHub
{
    internal class TemplateData
    {
        public static readonly Dictionary<Type, IEnumerable<object>> DataNodeData
            = new()
            {
                { typeof(InsurancePortfolio), new []
                {
                    new InsurancePortfolio{SystemName="DT",DisplayName="DT Complex CF",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF"},
                    new InsurancePortfolio{SystemName="DT1",DisplayName="DT1 OCI",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF"},
                    new InsurancePortfolio{SystemName="DT2",DisplayName="DT2 NOCI",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="null",ReportingNode="CH",FunctionalCurrency="CHF"},
                    new InsurancePortfolio{SystemName="DT3",DisplayName="DT3 RunOff",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF"},
                    new InsurancePortfolio{SystemName="DT4",DisplayName="DT4 OCI",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF"},
                    new InsurancePortfolio{SystemName="DT5",DisplayName="DT5 Simple Import",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF"},
                    new InsurancePortfolio{SystemName="DT10",DisplayName="DT10 PPA",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="PAA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF"},
                }},
                { typeof(GroupOfContract), new []
                {
                    new GroupOfInsuranceContract {SystemName="DT1.1",DisplayName="DT1.1 OCI LRC PA 0.8",Portfolio="DT1",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",YieldCurveName=null,},
                    new GroupOfInsuranceContract {SystemName="DT1.2",DisplayName="DT1.2 OCI LIC",Portfolio="DT1",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LIC",Profitability="P",YieldCurveName=null,},
                    new GroupOfInsuranceContract {SystemName="DT1.3",DisplayName="DT1.3 OCI LRC PA 1",Portfolio="DT1",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",YieldCurveName=null,},
                    new GroupOfInsuranceContract {SystemName="DT1.4",DisplayName="DT1.4 Adv and Ove Actuals on DT1.1",Portfolio="DT1",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",YieldCurveName=null,},
                    new GroupOfInsuranceContract {SystemName="DT1.5",DisplayName="DT1.5 OA and WO Premium on DT1.1",Portfolio="DT1",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",YieldCurveName=null,},
                    new GroupOfInsuranceContract {SystemName="DT2.1",DisplayName="DT2.1 NOCI LRC PA 0.8",Portfolio="DT2",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="null",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",YieldCurveName=null,},
                    new GroupOfInsuranceContract {SystemName="DT2.2",DisplayName="DT2.2 NOCI LIC",Portfolio="DT2",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="null",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LIC",Profitability="P",YieldCurveName=null,},
                    new GroupOfInsuranceContract {SystemName="DT3.1",DisplayName="DT3.1 Runoff - PA 0.8",Portfolio="DT3",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",YieldCurveName=null,},
                    new GroupOfInsuranceContract {SystemName="DT4.1",DisplayName="DT4.1 CSM PA 0.8",Portfolio="DT4",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",YieldCurveName=null,},
                    new GroupOfInsuranceContract {SystemName="DT5.1",DisplayName="DT5.1 Simple Import on DT 4.1",Portfolio="DT5",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",YieldCurveName=null,},
                    new GroupOfInsuranceContract {SystemName="DTP1.1",DisplayName="DTP1.1 Projection",Portfolio="DT1",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",YieldCurveName="NoDiscount",},
                    new GroupOfInsuranceContract {SystemName="DT10.1",DisplayName="DT10.1 PAA",Portfolio="DT10",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="PAA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LIC",Profitability="P",YieldCurveName=null,},
                    new GroupOfInsuranceContract {SystemName="DT10.2",DisplayName="DT10.1 PAA",Portfolio="DT10",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="PAA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",YieldCurveName=null,},
                }},
                { typeof(ReinsurancePortfolio), new []
                {
                    new ReinsurancePortfolio{SystemName="DTR",DisplayName="DTR complex CF",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType=null,ReportingNode="CH",FunctionalCurrency="CHF"},
                    new ReinsurancePortfolio{SystemName="DTR1",DisplayName="DTR1 OCI",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",ReportingNode="CH",FunctionalCurrency="CHF"},
                    new ReinsurancePortfolio{SystemName="DTR2",DisplayName="DTR2 NOCI",ContractualCurrency="USD",LineOfBusiness="ANN",ValuationApproach="BBA",OciType=null,ReportingNode="CH",FunctionalCurrency="CHF"},
                }},
                {typeof(GroupOfReinsuranceContract), new []
                {
                    new GroupOfReinsuranceContract{SystemName="DTR1.1",DisplayName="DTR1.1 OCI LRC",Portfolio="DTR1",ContractualCurrency="USD",FunctionalCurrency="CHF",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",Partner="PT1",},
                    new GroupOfReinsuranceContract{SystemName="DTR1.2",DisplayName="DTR1.2 OCI LIC",Portfolio="DTR1",ContractualCurrency="USD",FunctionalCurrency="CHF",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",AnnualCohort=2020,LiabilityType="LIC",Profitability="P",Partner="PT1",},
                    new GroupOfReinsuranceContract{SystemName="DTR1.3",DisplayName="DTR1.3 Adv and Ove Actuals on DTR1.1",Portfolio="DTR1",ContractualCurrency="USD",FunctionalCurrency="CHF",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",Partner="PT1",},
                    new GroupOfReinsuranceContract{SystemName="DTR1.4",DisplayName="DTR1.4 OA and WO Claims on DTR1.1",Portfolio="DTR1",ContractualCurrency="USD",FunctionalCurrency="CHF",LineOfBusiness="ANN",ValuationApproach="BBA",OciType="Default",AnnualCohort=2020,LiabilityType="LRC",Profitability="P",Partner="PT1",},
                    new GroupOfReinsuranceContract{SystemName="DTR2.1",DisplayName="DTR2.1 NOCI LRC",Portfolio="DTR2",ContractualCurrency="USD",FunctionalCurrency="CHF",LineOfBusiness="ANN",ValuationApproach="BBA",OciType=null,AnnualCohort=2020,LiabilityType="LRC",Profitability="P",Partner="PT1",},
                    new GroupOfReinsuranceContract{SystemName="DTR2.2",DisplayName="DTR2.2 NOCI LIC",Portfolio="DTR2",ContractualCurrency="USD",FunctionalCurrency="CHF",LineOfBusiness="ANN",ValuationApproach="BBA",OciType=null,AnnualCohort=2020,LiabilityType="LIC",Profitability="P",Partner="PT1",},
                }}
            };
    }
}
