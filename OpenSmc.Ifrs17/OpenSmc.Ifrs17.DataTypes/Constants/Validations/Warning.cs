namespace OpenSmc.Ifrs17.Domain.Constants.Validations;

public class Warning : ValidationBase
{
    protected const string DefaultMessage = "Warning not found.";

    protected Warning(string messageCode) : base(messageCode)
    {
    }

    public static readonly Warning ActiveDataNodeWithCashflowBOPI = new(nameof(ActiveDataNodeWithCashflowBOPI));
    public static readonly Warning VariablesAlreadyImported = new(nameof(VariablesAlreadyImported));
    public static readonly Warning VariablesAlreadyCalculated = new(nameof(VariablesAlreadyCalculated));
    public static readonly Warning ScenarioReCalculations = new(nameof(ScenarioReCalculations));
    public static readonly Warning MandatoryAocStepMissing = new(nameof(MandatoryAocStepMissing));

    // ImportStorage
    public static readonly Warning ReleasePatternNotFound = new(nameof(ReleasePatternNotFound));

    public static readonly Warning Generic = new(nameof(Generic));

    public override string GetMessage(params string[] s)
    {
        return (MessageCode, s.Length) switch
        {
            (nameof(ActiveDataNodeWithCashflowBOPI), 1) => $"Cash flow with AoC Type: {AocTypes.BOP} and Novelty: {Novelties.I} for Group of Contract {s[0]} is not allowed because previous period data are available.",
            (nameof(VariablesAlreadyImported), 0) => $"The import of the current file does not contain any new data. Hence, no data will be saved or calculations will be performed.",
            (nameof(MandatoryAocStepMissing), 3) => $"The AoC step ({s[0]}, {s[1]}) is not imported for ({s[2]}).",
            (nameof(ScenarioReCalculations), 1) => $"The present Best Estimate import makes the result of dependent Scenarios out of date. Hence, the following Scenarios are re-calculated: {s[0]}.",
            // ImportStorage
            (nameof(ReleasePatternNotFound), 2) => $"Release pattern for Group of Contract {s[0]} and AmountType {s[1]} is not found.",
            // Default
            (nameof(Generic), _) => $"{s[0]}",
            _ => DefaultMessage
        };
    }
}