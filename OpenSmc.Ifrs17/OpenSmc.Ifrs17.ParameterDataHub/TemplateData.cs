using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel;

namespace OpenSmc.Ifrs17.ParameterDataHub;

public static class TemplateData
{
    public static readonly ExchangeRate[] ExchangeRateData = new[]
    {
        new ExchangeRate()
        {
            Currency = "EUR", Year = 2021, Month = 3, FxType = FxType.Average, FxToGroupCurrency = 1.0212,
            
        },
        new ExchangeRate()
        {
            Currency = "EUR", Year = 2021, Month = 3, FxType = FxType.Spot, FxToGroupCurrency = 1.0213,
            
        },
        new ExchangeRate()
        {
            Currency = "EUR", Year = 2020, Month = 12, FxType = FxType.Average, FxToGroupCurrency = 1.0214,
            
        },
        new ExchangeRate()
        {
            Currency = "EUR", Year = 2020, Month = 12, FxType = FxType.Spot, FxToGroupCurrency = 1.0215,
            
        },

        new ExchangeRate()
        {
            Currency = "USD", Year = 2021, Month = 3, FxType = FxType.Average, FxToGroupCurrency = 1.0216,
            
        },
        new ExchangeRate()
        {
            Currency = "USD", Year = 2021, Month = 3, FxType = FxType.Spot, FxToGroupCurrency = 1.0217,
            
        },
        new ExchangeRate()
        {
            Currency = "USD", Year = 2020, Month = 12, FxType = FxType.Average, FxToGroupCurrency = 1.0218,
            
        },
        new ExchangeRate()
        {
            Currency = "USD", Year = 2020, Month = 12, FxType = FxType.Spot, FxToGroupCurrency = 1.0219,
            
        },

        new ExchangeRate()
        {
            Currency = "GBP", Year = 2021, Month = 3, FxType = FxType.Average, FxToGroupCurrency = 1.4016,
            
        },
        new ExchangeRate()
        {
            Currency = "GBP", Year = 2021, Month = 3, FxType = FxType.Spot, FxToGroupCurrency = 1.4017,
            
        },
        new ExchangeRate()
        {
            Currency = "GBP", Year = 2020, Month = 12, FxType = FxType.Average, FxToGroupCurrency = 1.4018,
            
        },
        new ExchangeRate()
        {
            Currency = "GBP", Year = 2020, Month = 12, FxType = FxType.Spot, FxToGroupCurrency = 1.4019,
            
        },
    };

    public static readonly Dictionary<Type, IEnumerable<object>> ParameterData
        = new()
        {
            {
                typeof(ExchangeRate), new[]
                {
                    new ExchangeRate()
                    {
                        Currency = "EUR", Year = 2021, Month = 3, FxType = FxType.Average, FxToGroupCurrency = 1.0212,
                        Id = new Guid()
                    },
                    new ExchangeRate()
                    {
                        Currency = "EUR", Year = 2021, Month = 3, FxType = FxType.Spot, FxToGroupCurrency = 1.0213,
                        Id = new Guid()
                    },
                    new ExchangeRate()
                    {
                        Currency = "EUR", Year = 2020, Month = 12, FxType = FxType.Average, FxToGroupCurrency = 1.0214,
                        Id = new Guid()
                    },
                    new ExchangeRate()
                    {
                        Currency = "EUR", Year = 2020, Month = 12, FxType = FxType.Spot, FxToGroupCurrency = 1.0215,
                        Id = new Guid()
                    },

                    new ExchangeRate()
                    {
                        Currency = "USD", Year = 2021, Month = 3, FxType = FxType.Average, FxToGroupCurrency = 1.0216,
                        Id = new Guid()
                    },
                    new ExchangeRate()
                    {
                        Currency = "USD", Year = 2021, Month = 3, FxType = FxType.Spot, FxToGroupCurrency = 1.0217,
                        Id = new Guid()
                    },
                    new ExchangeRate()
                    {
                        Currency = "USD", Year = 2020, Month = 12, FxType = FxType.Average, FxToGroupCurrency = 1.0218,
                        Id = new Guid()
                    },
                    new ExchangeRate()
                    {
                        Currency = "USD", Year = 2020, Month = 12, FxType = FxType.Spot, FxToGroupCurrency = 1.0219,
                        Id = new Guid()
                    },

                    new ExchangeRate()
                    {
                        Currency = "GBP", Year = 2021, Month = 3, FxType = FxType.Average, FxToGroupCurrency = 1.4016,
                        Id = new Guid()
                    },
                    new ExchangeRate()
                    {
                        Currency = "GBP", Year = 2021, Month = 3, FxType = FxType.Spot, FxToGroupCurrency = 1.4017,
                        Id = new Guid()
                    },
                    new ExchangeRate()
                    {
                        Currency = "GBP", Year = 2020, Month = 12, FxType = FxType.Average, FxToGroupCurrency = 1.4018,
                        Id = new Guid()
                    },
                    new ExchangeRate()
                    {
                        Currency = "GBP", Year = 2020, Month = 12, FxType = FxType.Spot, FxToGroupCurrency = 1.4019,
                        Id = new Guid()
                    },
                }
            },
            {
                typeof(CreditDefaultRate), new[]
                {
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "AAA", Values = new[] { 0.0004 }, Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "AA+", Values = new[] { 0.000242487 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "AA", Values = new[] { 0.00042 }, Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "AA-", Values = new[] { 0.000469849 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "A+", Values = new[] { 0.000525615 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "A", Values = new[] { 0.000588 }, Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "A-", Values = new[] { 0.000853615 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "BBB+", Values = new[] { .001239215 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "BBB", Values = new[] { 0.001799 }, Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "BBB-", Values = new[] { 0.00297649 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "BB+", Values = new[] { 0.004924677 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "BB", Values = new[] { 0.008148 }, Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "BB-", Values = new[] { 0.011522675 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "B+", Values = new[] { 0.016295046 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "B", Values = new[] { 0.023044 }, Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "B-", Values = new[] { 0.031505634 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "CCC+", Values = new[] { 0.043074334 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "CCC", Values = new[] { 0.058891 }, Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "CCC", Values = new[] { 0.058891 }, Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "CCC-", Values = new[] { 0.079972327 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "CC", Values = new[] { 0.108600179 },
                        Id = new Guid()
                    },
                    new CreditDefaultRate()
                    {
                        Year = 1900, Month = 12, CreditRiskRating = "C", Values = new[] { 0.147476 }, Id = new Guid()
                    },
                    new CreditDefaultRate()
                        { Year = 1900, Month = 12, CreditRiskRating = "I", Values = new[] { 0.0 }, Id = new Guid() },
                }
            },
            {typeof(PartnerRating), new[]
            {
                new PartnerRating()
                    { Partner = "PT1", CreditRiskRating = "AAA", Year = 2020, Month = 12, Id = new Guid() },
                new PartnerRating()
                    { Partner = "PT1", CreditRiskRating = "BBB", Year = 2021, Month = 3, Id = new Guid() },
                new PartnerRating()
                    { Partner = "PT1", CreditRiskRating = "I", Year = 2019, Month = 12, Id = new Guid() },
            }
        },
};

}