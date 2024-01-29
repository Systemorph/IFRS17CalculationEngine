namespace OpenSmc.Ifrs17.Domain.Constants.Validations;

public class Error : ValidationBase
{
    protected const string DefaultMessage = "Error not found.";

    protected Error(string messageCode) : base(messageCode)
    {
    }

    // Import Errors
    public static readonly Error NoMainTab = new(nameof(NoMainTab));
    public static readonly Error IncompleteMainTab = new(nameof(IncompleteMainTab));
    public static readonly Error ParsingInvalidOrScientificValue = new(nameof(ParsingInvalidOrScientificValue));
    public static readonly Error ValueTypeNotFound = new(nameof(ValueTypeNotFound));
    public static readonly Error ValueTypeNotValid = new(nameof(ValueTypeNotValid));
    public static readonly Error ReportingNodeInMainNotFound = new(nameof(ReportingNodeInMainNotFound));
    public static readonly Error YearInMainNotFound = new(nameof(YearInMainNotFound));
    public static readonly Error MonthInMainNotFound = new(nameof(MonthInMainNotFound));
    public static readonly Error ScenarioInMainNotAvailable = new(nameof(ScenarioInMainNotAvailable));
    public static readonly Error AocTypeNotValid = new(nameof(AocTypeNotValid));
    public static readonly Error AocTypeCompulsoryNotFound = new(nameof(AocTypeCompulsoryNotFound));
    public static readonly Error AocTypePositionNotSupported = new(nameof(AocTypePositionNotSupported));
    public static readonly Error AocConfigurationOrderNotUnique = new(nameof(AocConfigurationOrderNotUnique));
    public static readonly Error AccidentYearTypeNotValid = new(nameof(AccidentYearTypeNotValid));
    public static readonly Error TableNotFound = new(nameof(TableNotFound));

    // Partition Errors
    public static readonly Error PartitionNotFound = new(nameof(PartitionNotFound));
    public static readonly Error ParsedPartitionNotFound = new(nameof(ParsedPartitionNotFound));
    public static readonly Error PartititionNameNotFound = new(nameof(PartititionNameNotFound));
    public static readonly Error PartitionTypeNotFound = new(nameof(PartitionTypeNotFound));

    // Dimensions Errors
    public static readonly Error AmountTypeNotFound = new(nameof(AmountTypeNotFound));
    public static readonly Error EstimateTypeNotFound = new(nameof(EstimateTypeNotFound));
    public static readonly Error ReportingNodeNotFound = new(nameof(ReportingNodeNotFound));
    public static readonly Error AocTypeMapNotFound = new(nameof(AocTypeMapNotFound));
    public static readonly Error AocTypeNotFound = new(nameof(AocTypeNotFound));
    public static readonly Error PortfolioGicNotFound = new(nameof(PortfolioGicNotFound));
    public static readonly Error PortfolioGricNotFound = new(nameof(PortfolioGricNotFound));
    public static readonly Error InvalidAmountTypeEstimateType = new(nameof(InvalidAmountTypeEstimateType));
    public static readonly Error MultipleTechnicalMarginOpening = new(nameof(MultipleTechnicalMarginOpening));
    public static readonly Error DimensionNotFound = new(nameof(DimensionNotFound));
    public static readonly Error NoScenarioOpening = new(nameof(NoScenarioOpening));

    // Exchange Rate Errors
    public static readonly Error ExchangeRateNotFound = new(nameof(ExchangeRateNotFound));
    public static readonly Error ExchangeRateCurrency = new(nameof(ExchangeRateCurrency));

    // Data Node State Errors
    public static readonly Error ChangeDataNodeState = new(nameof(ChangeDataNodeState));
    public static readonly Error InactiveDataNodeState = new(nameof(InactiveDataNodeState));

    // Parameters Errors
    public static readonly Error ReinsuranceCoverageDataNode = new(nameof(ReinsuranceCoverageDataNode));
    public static readonly Error DuplicateInterDataNode = new(nameof(DuplicateInterDataNode));
    public static readonly Error DuplicateSingleDataNode = new(nameof(DuplicateSingleDataNode));
    public static readonly Error MissingSingleDataNodeParameter = new(nameof(MissingSingleDataNodeParameter));
    public static readonly Error InvalidDataNode = new(nameof(InvalidDataNode));
    public static readonly Error InvalidDataNodeForOpening = new(nameof(InvalidDataNodeForOpening));
    public static readonly Error InvalidCashFlowPeriodicity = new(nameof(InvalidCashFlowPeriodicity));
    public static readonly Error MissingInterpolationMethod = new(nameof(MissingInterpolationMethod));
    public static readonly Error InvalidInterpolationMethod = new(nameof(InvalidInterpolationMethod));
    public static readonly Error InvalidEconomicBasisDriver = new(nameof(InvalidEconomicBasisDriver));
    public static readonly Error InvalidReleasePattern = new(nameof(InvalidReleasePattern));

    // Storage Errors
    public static readonly Error DataNodeNotFound = new(nameof(DataNodeNotFound));
    public static readonly Error PartnerNotFound = new(nameof(PartnerNotFound));
    public static readonly Error PeriodNotFound = new(nameof(PeriodNotFound));
    public static readonly Error RatingNotFound = new(nameof(RatingNotFound));
    public static readonly Error CreditDefaultRateNotFound = new(nameof(CreditDefaultRateNotFound));
    public static readonly Error MissingPremiumAllocation = new(nameof(MissingPremiumAllocation));
    public static readonly Error ReinsuranceCoverage = new(nameof(ReinsuranceCoverage));
    public static readonly Error YieldCurveNotFound = new(nameof(YieldCurveNotFound));
    public static readonly Error YieldCurvePeriodNotApplicable = new(nameof(YieldCurvePeriodNotApplicable));
    public static readonly Error EconomicBasisNotFound = new(nameof(EconomicBasisNotFound));
    public static readonly Error AccountingVariableTypeNotFound = new(nameof(AccountingVariableTypeNotFound));
    public static readonly Error InvalidGric = new(nameof(InvalidGric));
    public static readonly Error InvalidGic = new(nameof(InvalidGic));
    public static readonly Error ReleasePatternNotFound = new(nameof(ReleasePatternNotFound));
    public static readonly Error MissingPreviousPeriodData = new(nameof(MissingPreviousPeriodData));

    // Scopes Errors; Range 110 -119
    public static readonly Error NotSupportedAocStepReference = new(nameof(NotSupportedAocStepReference));
    public static readonly Error MultipleEoP = new(nameof(MultipleEoP));

    // Data Completeness Errors; Range 120 - 129
    public static readonly Error MissingDataAtPosting = new(nameof(MissingDataAtPosting));
    public static readonly Error MissingCombinedLiability = new(nameof(MissingCombinedLiability));
    public static readonly Error MissingCoverageUnit = new(nameof(MissingCoverageUnit));

    // Index Error 
    public static readonly Error NegativeIndex = new(nameof(NegativeIndex));

    // Generic Errors
    public static readonly Error Generic = new(nameof(Generic));

    public override string GetMessage(params string?[] s)
    {
        return (MessageCode, s.Length) switch
        {
            // Import
            (nameof(NoMainTab), 0) => $"No Main tab in the parsed file.",
            (nameof(IncompleteMainTab), _) => $"Incomplete Main tab in the parsed file.",
            (nameof(ParsingInvalidOrScientificValue), 1) => $"While parsing found invalid value or real number in scientific notation: {s[0]}.",
            (nameof(ValueTypeNotFound), _) => $"Value Type not found.",
            (nameof(ValueTypeNotValid), 1) => $"The Value Type {s[0]} is invalid.",
            (nameof(ReportingNodeInMainNotFound), _) => $"Reporting Node missing from the Main tab.",
            (nameof(YearInMainNotFound), _) => $"Year missing from the Main tab.",
            (nameof(MonthInMainNotFound), _) => $"Month missing from the Main tab.",
            (nameof(ScenarioInMainNotAvailable), 1) => $"Scenario {s[0]} has not been defined.",
            (nameof(AocTypeNotValid), 1) => $"The parsed AoC Type {s[0]} is invalid.",
            (nameof(AocTypeCompulsoryNotFound), _) => $"Not all compulsory AoC Types have been imported.",
            (nameof(AocTypePositionNotSupported), 1) => $"The position of the AoC Type {s[0]} is not supported.",
            (nameof(AocConfigurationOrderNotUnique), _) => $"Two or more AoC Configurations have the same Order.",
            (nameof(AccidentYearTypeNotValid), 1) => $"The parsed AccidentYear {s[0]} is invalid. Expected Accident Year input of type int.",
            (nameof(TableNotFound), 1) => $"The import file does not contain table {s[0]}",
            (nameof(TableNotFound), 2) => $"The import file contains neither table {s[0]} nor {s[1]}",

            // Partition
            (nameof(PartitionNotFound), _) => $"Partition do not found.",
            (nameof(ParsedPartitionNotFound), 1) => $"Parsed partition not available: ReportingNode {s[0]}.",
            (nameof(ParsedPartitionNotFound), 4) => $"Parsed partition not available: ReportingNode {s[0]}, Year {s[1]}, Month {s[2]}, Scenario {s[3]}.",
            (nameof(PartitionTypeNotFound), 1) => $"Partition type not found: {s[0]}.",

            // Dimensions
            (nameof(AmountTypeNotFound), 1) => $"AmountType {s[0]} not found.",
            (nameof(EstimateTypeNotFound), 1) => $"EstimateType {s[0]} not found.",
            (nameof(ReportingNodeNotFound), 1) => $"Reporting Node {s[0]} not found.",
            (nameof(AocTypeNotFound), 1) => $"AoC Type {s[0]} not found.",
            (nameof(AocTypeMapNotFound), 2) => $"AoC Type {s[0]} and Novelty {s[1]} combination not defined in the mapping.",
            (nameof(PortfolioGicNotFound), 2) => $"Portfolio {s[0]} assigned to Group of Insurance Contract {s[1]} does not exist.",
            (nameof(PortfolioGricNotFound), 2) => $"Portfolio {s[0]} assigned to Group of Reinsurance Contract {s[1]} does not exist.",
            (nameof(InvalidAmountTypeEstimateType), 2) => $"Invalid combination of EstimateType {s[0]} and AmountType {s[1]}.",
            (nameof(MultipleTechnicalMarginOpening), 1) => $"Multiple opening for techincal margin are not allowed for DataNode {s[0]}.",
            (nameof(DimensionNotFound), 2) => $"Column {0} has unknown value {1}.",
            (nameof(NoScenarioOpening), 0) => "Only Best Estimate is valid Scenario for Openings",

            // Exchange Rate
            (nameof(ExchangeRateNotFound), 2) => $"Exchange Rate for {s[0]} {s[1]} is not present.",
            (nameof(ExchangeRateCurrency), 1) => $"{s[0]} does not have any Exchange Rate defined.",

            // Data Node State
            (nameof(ChangeDataNodeState), 0) => $"Data Node State can not change from Inactive state into Active state.",
            (nameof(ChangeDataNodeState), 1) => $"Data Node State for {s[0]} can not change from Inactive state into Active state.",
            (nameof(ChangeDataNodeState), 3) => $"Data Node State for {s[0]} can not change from {s[1]} state into {s[2]} state.",
            (nameof(InactiveDataNodeState), 1) => $"Data imported for inactive Data Node {s[0]}.",

            //Parameters
            (nameof(ReinsuranceCoverageDataNode), 2) => $"Invalid Reinsurance Coverage parameter does not link a GroupOfReinsuranceContract to a GroupOfInsuranceContract. Provided GroupOfContracts are: {s[0]}, {s[1]}.",
            (nameof(DuplicateInterDataNode), 2) => $"Duplicated Inter-DataNode parameter for {s[0]}-{s[1]} is found.",
            (nameof(DuplicateSingleDataNode), 1) => $"Duplicated Single-DataNode parameter for {s[0]} is found.",
            (nameof(MissingSingleDataNodeParameter), 1) => $"Single DataNode Parameter for Data Node {s[0]} is not found.",
            (nameof(InvalidDataNode), 1) => $"Data imported for invalid Data Node {s[0]}.",
            (nameof(InvalidDataNodeForOpening), 1) => $"Data imported for invalid Data Node or for a Data Node after its inception year {s[0]}.",
            (nameof(InvalidCashFlowPeriodicity), 1) => $"Invalid CashFlowPeriodicity parameter for Data Node {s[0]}.",
            (nameof(MissingInterpolationMethod), 1) => $"Missing InterpolationMethod parameter for Data Node {s[0]}.",
            (nameof(InvalidInterpolationMethod), 1) => $"Invalid InterpolationMethod parameter for Data Node {s[0]}.",
            (nameof(InvalidEconomicBasisDriver), 1) => $"Invalid EconomicBasisDriver parameter for Data Node {s[0]}.",
            (nameof(InvalidReleasePattern), 1) => $"Invalid ReleasePattern parameters for Data Node {s[0]}.",

            // Storage
            (nameof(DataNodeNotFound), 1) => $"DataNode {s[0]} not found.",
            (nameof(PartnerNotFound), 1) => $"Partner not found for DataNode {s[0]}.",
            (nameof(PeriodNotFound), 1) => $"Invalid Period {s[0]} used during calculation. Allowed values are Current Period (0) and Previous Period (-1).",
            (nameof(RatingNotFound), 1) => $"Rating not found for Partner {s[0]}.",
            (nameof(CreditDefaultRateNotFound), 1) => $"Credit Default Rate not found for rating {s[0]}.",
            (nameof(MissingPremiumAllocation), 1) => $"Premium Allocation Rate not found for Group of Contract {s[0]}.", // TODO: this is now a warning to be produced by a validation in the importers (default is 1)
            (nameof(ReinsuranceCoverage), 1) => $"Reinsurance Allocation Rate not found for Group of Insurance Contract {s[0]}.",
            (nameof(YieldCurveNotFound), 6) => $"Yield Curve not found for DataNode {s[0]}, Currency {s[1]}, Year {s[2]}, Month {s[3]}, Scenario {(s[4] == null ? "Best Estimate" : s[4])} and Name {s[5]}.",
            (nameof(YieldCurvePeriodNotApplicable), 2) => $"YieldCurve period NotApplicable not valid for AoC Step with AoC Type {s[0]} and Novelty {s[1]}.",
            (nameof(EconomicBasisNotFound), 1) => $"EconomicBasis not valid for DataNode {s[0]}.",
            (nameof(AccountingVariableTypeNotFound), 1) => $"AccountingVariableType {s[0]} not found.",
            (nameof(InvalidGric), 1) => $"Invalid Group of Reinsurance Contract {s[0]} has been requested during calculation.",
            (nameof(InvalidGic), 1) => $"Invalid Group of Insurance Contract {s[0]} has been requested during calculation.",
            (nameof(ReleasePatternNotFound), 2) => $"Release pattern for Group of Contract {s[0]} and AmountType {s[1]} is not found.",
            (nameof(MissingPreviousPeriodData), 3) => $"Data for previous period (Year: {s[0]}, Month: {s[1]}) is missing for Group of contracts: {s[2]}.",

            // Scopes
            (nameof(NotSupportedAocStepReference), 1) => $"Unsupported reference AoC Step for AoC Type {s[0]}.",
            (nameof(MultipleEoP), 0) => $"Closing Balance for both Csm and Lc are computed.",

            // Data Completeness
            (nameof(MissingDataAtPosting), 1) => $"Missing imported data for {s[0]} DataNode.",
            (nameof(MissingCombinedLiability), 2) => $"Missing Combined Liability AoC Type for DataNode {s[0]} and AmountType {s[1]}.",
            (nameof(MissingCoverageUnit), 1) => $"Missing Coverage Unit cash flow for {s[0]} DataNode.",

            // Index
            (nameof(NegativeIndex), 0) => $"Index was out of range. Must be non-negative.",

            // Default
            (nameof(Generic), 1) => $"{s[0]}",
            _ => DefaultMessage
        };
    }
}