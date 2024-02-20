using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

/* AM (1.2.2024) todo list:
 *  a) organize better the IFRS17 project structure
 *  b) orchestrate which data go to which hub, e.g. refDataHub owns all dimensions
 *      write the financialDataConfiguration for parameters, transactionalData, etc etc
 *  c) finish setting up all model hubs simply by means of this generic DataPlugin
 *  d) implement tests for the DataPlugin by adding tests in the OpenSMC repo
 *  e) test the IFRS17 model hubs writing the level above this configurations
 *      to do this check the existing tests in the OpenSMC, e.g. MessageHubTest
 *      then write the financialDataConfiguration to define routing, addresses, forwarding, etc $
 *  f) think at the viewModelHub, what to do here? where to start?
 *      look at the existing tests in OpenSMC, e.g. LayoutTest
 *  g) monitor the development of the import/export plugin so that we can use them here
 *      in smc v1 
 */



public static class DataHubConfiguration
{

    public static MessageHubConfiguration ConfigureReferenceData(this MessageHubConfiguration configuration)
    {
        // TODO: this needs to be registered in the higher level


        return configuration.AddData(dc => dc.WithDataSource("ReferenceDataSource",
            ds => ds.WithType<AmountType>(t => t.WithKey(x => x.SystemName)
                        .WithInitialData(async () => await Task.FromResult(new List<AmountType>()))
                        .WithUpdate(AddAmountType)
                        .WithAdd(AddAmountType)
                        .WithDelete(RemoveAmountType)
                )
                .WithType<AocStep>(t => t.WithKey(x => (x.AocType, x.Novelty))
                    .WithInitialData(async () => await Task.FromResult(new List<AocStep>()))
                    .WithUpdate(AddAocStep)
                    .WithAdd(AddAocStep)
                    .WithDelete(RemoveAocStep))));
    }

    private static void RemoveAocStep(IReadOnlyCollection<AocStep> obj)
    {
        throw new NotImplementedException();
    }

    private static void AddAocStep(IReadOnlyCollection<AocStep> obj)
    {
        throw new NotImplementedException();
    }

    private static void RemoveAmountType(IReadOnlyCollection<AmountType> obj)
    {
        throw new NotImplementedException();
    }

    private static void AddAmountType(IReadOnlyCollection<AmountType> obj)
    {
        throw new NotImplementedException();
    }
}


