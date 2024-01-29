namespace OpenSmc.Ifrs17.Domain.DataModel.Interfaces;

public interface IWithYearMonthAndScenario : IWithYearAndMonth
{
    public string? Scenario { get; init; }
}