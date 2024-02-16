using OpenSmc.Data;
using OpenSmc.DataStructures;
using OpenSmc.Hub.Fixture;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel.Args;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;
using OpenSmc.Import;
using OpenSmc.Messaging;
using OpenSms.Ifrs17.CalculationScopes;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.ReferenceDataHub.Test
{
    public class NominalCashflowTest(ITestOutputHelper output) : HubTestBase(output)
    {
        protected override MessageHubConfiguration ConfigureHost(MessageHubConfiguration configuration) =>
            base.ConfigureHost(configuration).AddData(data => data.WithDataSource(nameof(DataSource),
                    source => source.WithType<RawVariable>(t => t.WithKey(x =>
                            (x.ReportingNode, x.Year, x.AmountType, x.Novelty, x.Month, x.DataNode, x.EstimateType))
                        .WithInitialData(_referenceRawVariable[typeof(RawVariable)].Cast<RawVariable>()))
                        .WithType<AocType>(t => t.WithKey(x => x.SystemName))))
                .AddImport(imp => imp.WithFormat(nameof(RawVariable), 
                    format => format.WithImportFunction(CalculateIfrsVariable)));

        private readonly Dictionary<Type, IEnumerable<object>> _referenceRawVariable =
            new() {{typeof(RawVariable), new[]{new RawVariable()
            {
                EstimateType = "BE",
                DataNode = "DT10.2",
                AmountType = "DAE",
                AocType = "BOP",
                Novelty = "N",
                ReportingNode = "DE",
                Year = 2020,
                Month = 12,
                Scenario = null,
                Values = [15, 20, 25]
            }}}, {typeof(AocType), new[]{new AocType()
            {
                SystemName = "BOP",
                DisplayName = "Opening Balance", 
                Order = 10
            }}}};

        private IEnumerable<object> CalculateIfrsVariable(ImportRequest request,
            IDataSet dataSet, IMessageHub hub, IWorkspace workspace)
        {
            var importArgs = new ImportArgs("DE", 2020, 12, Periodicity.Monthly, null, null);
            var importStorage = new ImportStorage(importArgs);
            return new List<object>();
        } 
    }
}
