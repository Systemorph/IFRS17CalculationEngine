﻿using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.DataModel.Interfaces;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.IfrsVariableHub;

public static class IfrsVariableHubConfiguration
{
    public static MessageHubConfiguration ConfigureIfrsDataDictInit(this MessageHubConfiguration hostConfiguration, int year, int month, string reportingNode, string scenario) =>
        hostConfiguration
            .AddData(dc => dc
                .FromConfigurableDataSource(new IfrsVariableDataAddress(year, month, reportingNode, scenario, hostConfiguration), ds => ds
                    .WithType<IfrsVariable>(t => t
                        .WithKey(iv => (iv.ReportingNode, iv.Year, iv.Month, iv.Scenario, iv.EconomicBasis, iv.EstimateType, iv.AmountType, iv.AccidentYear, iv.DataNode, iv.AocType, iv.Novelty))
                        .WithInitialData(TemplateData.SimpleValueReferenceData.Where(iv => iv.Year == year && iv.Month == month && iv.ReportingNode == reportingNode && iv.Scenario == scenario)))));
}

public record IfrsVariableDataAddress(int Year, int month, string reportingNode, string scenario, object Host) : IHostedAddress;

