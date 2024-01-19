public enum FxType { Spot, Average }


public enum FxPeriod { NotApplicable, BeginningOfPeriod, Average, EndOfPeriod }


public enum CurrencyType { Functional, Group, Contractual, Transactional }


public enum PeriodType { NotApplicable, BeginningOfPeriod, EndOfPeriod }


public enum ValuationPeriod { NotApplicable, BeginningOfPeriod, MidOfPeriod, Delta, EndOfPeriod }


[Flags] public enum PortfolioView { Gross = 1, Reinsurance = 2, Net = Gross | Reinsurance }


[Flags]
public enum StructureType { None = 1, AocPresentValue = 2, AocAccrual = 4, AocTechnicalMargin = 8 }
//Combination in use in AocConfiguration:
//AocPresentValue | AocAccrual | AocTechnicalMargin = 14 : BOP,I
//AocPresentValue | AocTechnicalMargin = 10 : IA,I


public enum State { Active, Inactive }


public enum Periodicity { Monthly, Quarterly, Yearly }


public enum CashFlowPeriodicity { Monthly, Quarterly, Yearly }


public enum InterpolationMethod { NotApplicable, Uniform, Start, /*End  , Linear, Custom*/ }


[Flags]
public enum InputSource {NotApplicable = 0, Opening = 1, Actual = 2, Cashflow = 4} 
//Opening + Actual = 3,
//Opening + Cashflow = 5
//Actual + Cashflow = 6
//Opening + Actual + Cashflow = 7


[Flags]
public enum DataType {Optional = 1, Mandatory = 2, Calculated = 4, CalculatedTelescopic = 8, CalculatedProjection = 16}
//Combination in use in AocConfiguration:

//Optional + CalculatedProjection = 17 : used for AoC Step that are optional input and calculated in projections (eg BOP,I - CF,C - WO,C)
//Calculated + CalculatedProjection = 20  : used for AoC Step that are calculated in current period (if the data allows) and are certainly calculated in projections



public enum ImportScope { Primary, AddedToPrimary, Secondary }


public enum ServicePeriod{ NotApplicable, PastService, CurrentService, FutureService }
