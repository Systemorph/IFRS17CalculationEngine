namespace OpenSmc.Ifrs17.DataTypes.DataModel.Interfaces;

public interface IWithYearMonthAndScenario : IWithYearAndMonth
{
    public string? Scenario { get; init; }
}