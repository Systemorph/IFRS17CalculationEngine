#!import "../Utils/Queries"
#!import "../Utils/ImportCalculationMethods"
#!import "../Report/ReportConfigurationAndUtils"
#!import "TestData"


await DataSource.DeleteAsync( DataSource.Query<RawVariable>());
await DataSource.DeleteAsync( DataSource.Query<IfrsVariable>());
await DataSource.UpdateAsync( new[] { partitionReportingNode } );
await DataSource.UpdateAsync( new[] { partition, previousPeriodPartition, partitionScenarioMTUP, previousPeriodPartitionScenarioMTUP } );
await DataSource.UpdateAsync( new[] { dt11 } );
await Import.FromString(projectionConfiguration).WithType<ProjectionConfiguration>().WithTarget(DataSource).ExecuteAsync();


var bestEstimateRawVars = new[] { new RawVariable { AmountType = "PR", AocType = "CL", Novelty = "I", Partition = partition.Id, Values = new[] {1.0} }, 
                                  new RawVariable { AmountType = "PR", AocType = "AU", Novelty = "I", Partition = partition.Id, Values = new[] {2.0} },
                                  new RawVariable { AmountType = "PR", AocType = "EV", Novelty = "I", Partition = partition.Id, Values = new[] {3.0} },
                                  new RawVariable { AmountType = "CL", AocType = "CL", Novelty = "I", Partition = partition.Id, Values = new[] {4.0} },
                                  new RawVariable { AmountType = "CL", AocType = "AU", Novelty = "I", Partition = partition.Id, Values = new[] {5.0} },
                                  new RawVariable { AmountType = "CL", AocType = "EV", Novelty = "I", Partition = partition.Id, Values = new[] {6.0} } };


var previousScenarioRawVars = new[] { new RawVariable { AmountType = "PR", AocType = "CL", Novelty = "I", Partition = previousPeriodPartitionScenarioMTUP.Id, Values = new[] {3.15} }, 
                                      new RawVariable { AmountType = "PR", AocType = "AU", Novelty = "I", Partition = previousPeriodPartitionScenarioMTUP.Id, Values = new[] {7.17} } };


var scenarioRawVars = new[] { new RawVariable { AmountType = "PR", AocType = "CL", Novelty = "I", Partition = partitionScenarioMTUP.Id, Values = new[] {1.1} }, 
                              new RawVariable { AmountType = "PR", AocType = "AU", Novelty = "I", Partition = partitionScenarioMTUP.Id, Values = new[] {2.1} } };


var newScenarioRawVars = new[] { new RawVariable { AmountType = "PR", AocType = "CL", Novelty = "I", Partition = partitionScenarioMTUP.Id, Values = new[] {11.0} }, 
                                 new RawVariable { AmountType = "CL", AocType = "CL", Novelty = "I", Partition = partitionScenarioMTUP.Id, Values = new[] {41.0} } };


await DataSource.DeleteAsync( await DataSource.Query<RawVariable>().ToArrayAsync() );
await DataSource.UpdateAsync( bestEstimateRawVars.Concat(scenarioRawVars).ToArray() );
var ws = Workspace.CreateNew();
ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());
await ws.UpdateAsync(newScenarioRawVars);


var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable,PartitionByReportingNodeAndPeriod>(DataSource, partitionScenarioMTUP.Id, partition.Id, ImportFormats.Cashflow);


queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(52.0);


await DataSource.DeleteAsync( await DataSource.Query<RawVariable>().ToArrayAsync() );
await DataSource.UpdateAsync( bestEstimateRawVars.Concat(scenarioRawVars).ToArray() );
var ws = Workspace.CreateNew();
ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());


var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable,PartitionByReportingNodeAndPeriod>(DataSource, partitionScenarioMTUP.Id, partition.Id, ImportFormats.Actual);


queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(0);


await DataSource.DeleteAsync( await DataSource.Query<RawVariable>().ToArrayAsync() );
await DataSource.UpdateAsync( bestEstimateRawVars.Concat(scenarioRawVars).ToArray() );
var ws = Workspace.CreateNew();
ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());


var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable,PartitionByReportingNodeAndPeriod>(DataSource, partitionScenarioMTUP.Id, partition.Id, ImportFormats.Cashflow);


queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(3.2);


await DataSource.DeleteAsync( await DataSource.Query<RawVariable>().ToArrayAsync() );
await DataSource.UpdateAsync( bestEstimateRawVars.ToArray() );
var ws = Workspace.CreateNew();
ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());


var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable,PartitionByReportingNodeAndPeriod>(DataSource, partitionScenarioMTUP.Id, partition.Id, ImportFormats.Cashflow);


queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(21);


await DataSource.DeleteAsync( await DataSource.Query<RawVariable>().ToArrayAsync() );
await DataSource.UpdateAsync( bestEstimateRawVars.Concat(previousScenarioRawVars).ToArray() );
var ws = Workspace.CreateNew();
ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());


var queriedRawVars = await ws.QueryPartitionedDataAsync<RawVariable,PartitionByReportingNodeAndPeriod>(DataSource, partitionScenarioMTUP.Id, partition.Id, ImportFormats.Cashflow);


queriedRawVars.SelectMany(x => x.Values).Sum().Should().Be(21);


var bestEstimateIfrsVars = new[] { new IfrsVariable { AmountType = "PR", AocType = "CL", Novelty = "I", Partition = partition.Id, Values = SetProjectionValue(1.0) }, 
                                   new IfrsVariable { AmountType = "PR", AocType = "AU", Novelty = "I", Partition = partition.Id, Values = SetProjectionValue(2.0) },
                                   new IfrsVariable { AmountType = "PR", AocType = "EV", Novelty = "I", Partition = partition.Id, Values = SetProjectionValue(3.0)},
                                   new IfrsVariable { AmountType = "CL", AocType = "CL", Novelty = "I", Partition = partition.Id, Values = SetProjectionValue(4.0)},
                                   new IfrsVariable { AmountType = "CL", AocType = "AU", Novelty = "I", Partition = partition.Id, Values = SetProjectionValue(5.0)},
                                   new IfrsVariable { AmountType = "CL", AocType = "EV", Novelty = "I", Partition = partition.Id, Values = SetProjectionValue(6.0)} };


var previousScenarioIfrsVars = new[] { new IfrsVariable { AmountType = "PR", AocType = "CL", Novelty = "I", Partition = previousPeriodPartitionScenarioMTUP.Id, Values = SetProjectionValue(3.15) }, 
                                       new IfrsVariable { AmountType = "PR", AocType = "AU", Novelty = "I", Partition = previousPeriodPartitionScenarioMTUP.Id, Values = SetProjectionValue(7.17) } };


var scenarioIfrsVars = new[] { new IfrsVariable { AmountType = "PR", AocType = "CL", Novelty = "I", Partition = partitionScenarioMTUP.Id, Values = SetProjectionValue(1.1) }, 
                               new IfrsVariable { AmountType = "PR", AocType = "AU", Novelty = "I", Partition = partitionScenarioMTUP.Id, Values = SetProjectionValue(2.1) } };


var newScenarioIfrsVars = new[] { new IfrsVariable { AmountType = "PR", AocType = "CL", Novelty = "I", Partition = partitionScenarioMTUP.Id, Values = SetProjectionValue(11.0) }, 
                                  new IfrsVariable { AmountType = "CL", AocType = "CL", Novelty = "I", Partition = partitionScenarioMTUP.Id, Values = SetProjectionValue(41.0) } };


await DataSource.DeleteAsync( await DataSource.Query<IfrsVariable>().ToArrayAsync() );
await DataSource.UpdateAsync( bestEstimateIfrsVars.Concat(scenarioIfrsVars).ToArray() );
var ws = Workspace.CreateNew();
ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());
await ws.UpdateAsync(newScenarioIfrsVars);


var queriedIfrsVars = await ws.QueryPartitionedDataAsync<IfrsVariable,PartitionByReportingNodeAndPeriod>(DataSource, partitionScenarioMTUP.Id, partition.Id, ImportFormats.Actual);


queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(52.0);


await DataSource.DeleteAsync( await DataSource.Query<IfrsVariable>().ToArrayAsync() );
await DataSource.UpdateAsync( bestEstimateIfrsVars.Concat(scenarioIfrsVars).ToArray() );
var ws = Workspace.CreateNew();
ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());


var queriedIfrsVars = await ws.QueryPartitionedDataAsync<IfrsVariable,PartitionByReportingNodeAndPeriod>(DataSource, partitionScenarioMTUP.Id, partition.Id, ImportFormats.Cashflow);


queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(0);


await DataSource.DeleteAsync( await DataSource.Query<IfrsVariable>().ToArrayAsync() );
await DataSource.UpdateAsync( bestEstimateIfrsVars.Concat(scenarioIfrsVars).ToArray() );
var ws = Workspace.CreateNew();
ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());


var queriedIfrsVars = await ws.QueryPartitionedDataAsync<IfrsVariable,PartitionByReportingNodeAndPeriod>(DataSource, partitionScenarioMTUP.Id, partition.Id, ImportFormats.Actual);


queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(3.2);


await DataSource.DeleteAsync( await DataSource.Query<IfrsVariable>().ToArrayAsync() );
await DataSource.UpdateAsync( bestEstimateIfrsVars.ToArray() );
var ws = Workspace.CreateNew();
ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());


var queriedIfrsVars = await ws.QueryPartitionedDataAsync<IfrsVariable,PartitionByReportingNodeAndPeriod>(DataSource, partitionScenarioMTUP.Id, partition.Id, ImportFormats.Actual);


queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(21.0);


await DataSource.DeleteAsync( await DataSource.Query<IfrsVariable>().ToArrayAsync() );
await DataSource.UpdateAsync( bestEstimateIfrsVars.Concat(previousScenarioIfrsVars).ToArray() );
var ws = Workspace.CreateNew();
ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());


var queriedIfrsVars = await ws.QueryPartitionedDataAsync<IfrsVariable,PartitionByReportingNodeAndPeriod>(DataSource, partitionScenarioMTUP.Id, partition.Id, ImportFormats.Actual);


queriedIfrsVars.Select(x => x.Values.Sum()).Sum().Should().Be(21);


var bestEstimateIfrsVars = new[] { new IfrsVariable { DataNode = "DT1.1", AmountType = "PR", AocType = "CL", Novelty = "I", Partition = partition.Id, Values = SetProjectionValue(1.0) }, 
                                   new IfrsVariable { DataNode = "DT1.1", AmountType = "PR", AocType = "AU", Novelty = "I", Partition = partition.Id, Values = SetProjectionValue(2.0) },
                                   new IfrsVariable { DataNode = "DT1.1", AmountType = "PR", AocType = "EV", Novelty = "I", Partition = partition.Id, Values = SetProjectionValue(3.0) },
                                   new IfrsVariable { DataNode = "DT1.1", AmountType = "CL", AocType = "CL", Novelty = "I", Partition = partition.Id, Values = SetProjectionValue(4.0) },
                                   new IfrsVariable { DataNode = "DT1.1", AmountType = "CL", AocType = "AU", Novelty = "I", Partition = partition.Id, Values = SetProjectionValue(5.0) },
                                   new IfrsVariable { DataNode = "DT1.1", AmountType = "CL", AocType = "EV", Novelty = "I", Partition = partition.Id, Values = SetProjectionValue(6.0) } };


var scenarioIfrsVars = new[] { new IfrsVariable { DataNode = "DT1.1", AmountType = "PR", AocType = "CL", Novelty = "I", Partition = partitionScenarioMTUP.Id, Values = SetProjectionValue(1.1) }, 
                               new IfrsVariable { DataNode = "DT1.1", AmountType = "PR", AocType = "AU", Novelty = "I", Partition = partitionScenarioMTUP.Id, Values = SetProjectionValue(2.1) } };


var ws = Workspace.CreateNew();
ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());


await ws.UpdateAsync(bestEstimateIfrsVars);
await ws.UpdateAsync(scenarioIfrsVars);


var projectionConfigurations = (await ws.Query<ProjectionConfiguration>().ToArrayAsync()).SortRelevantProjections(args.Month);


(await ws.QueryReportVariablesAsync((args.Year, args.Month, args.ReportingNode, args.Scenario), projectionConfigurations)).Select(x => x.Value).Sum().Should().Be(21);


var ws = Workspace.CreateNew();
ws.Initialize(x => x.FromSource(DataSource).DisableInitialization<RawVariable>().DisableInitialization<IfrsVariable>());


await ws.UpdateAsync(bestEstimateIfrsVars);
await ws.UpdateAsync(scenarioIfrsVars);


var projectionConfigurations = (await ws.Query<ProjectionConfiguration>().ToArrayAsync()).SortRelevantProjections(args.Month);


(await ws.QueryReportVariablesAsync((args.Year, args.Month, args.ReportingNode, argsScenarioMTUP.Scenario), projectionConfigurations)).Select(x => x.Value).Sum().Should().Be(21.2);



