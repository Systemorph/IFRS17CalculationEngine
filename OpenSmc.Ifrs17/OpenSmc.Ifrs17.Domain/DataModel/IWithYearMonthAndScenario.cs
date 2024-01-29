namespace OpenSmc.Ifrs17.Domain.DataModel;

public interface IWithYearMonthAndScenario : IWithYearAndMonth
{
    public string Scenario { get; init; }
}