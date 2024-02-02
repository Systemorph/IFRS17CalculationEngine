using OpenSmc.Ifrs17.Domain.DataModel.TransactionalData;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IEmptyIRevenue : IRevenueToIfrsVariable{
    IEnumerable<IfrsVariable> IRevenueToIfrsVariable.Revenue => Enumerable.Empty<IfrsVariable>();
    IEnumerable<IfrsVariable> IRevenueToIfrsVariable.RevenueAmFactor => Enumerable.Empty<IfrsVariable>();
}