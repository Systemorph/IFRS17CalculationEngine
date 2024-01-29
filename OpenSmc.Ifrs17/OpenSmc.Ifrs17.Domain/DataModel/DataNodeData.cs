using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record DataNodeData
{
    public string DataNode { get; init; }

    //Portfolio
    public string ContractualCurrency { get; init; }
    public string FunctionalCurrency { get; init; }
    public string LineOfBusiness { get; init; }
    public string ValuationApproach { get; init; }
    public string OciType { get; init; }

    //GroupOfContract
    public string Portfolio { get; init; }
    public int AnnualCohort { get; init; }
    public string LiabilityType { get; init; }
    public string Profitability { get; init; }
    public string Partner { get; init; }
    public string YieldCurveName { get; init; }


    //DataNodeState
    public int Year { get; init; }
    public int Month { get; init; }
    public State State { get; init; }
    public State PreviousState { get; init; }

    public bool IsReinsurance { get; init; }

    public DataNodeData()
    {
    }

    public DataNodeData(GroupOfContract dn)
    {
        DataNode = dn.SystemName;
        ContractualCurrency = dn.ContractualCurrency;
        FunctionalCurrency = dn.FunctionalCurrency;
        LineOfBusiness = dn.LineOfBusiness;
        ValuationApproach = dn.ValuationApproach;
        OciType = dn.OciType;
        Portfolio = dn.Portfolio;
        AnnualCohort = dn.AnnualCohort;
        LiabilityType = dn.LiabilityType;
        Profitability = dn.Profitability;
        Partner = dn.Partner;
        IsReinsurance = dn.GetType().Name == nameof(GroupOfReinsuranceContract);
        YieldCurveName = dn.YieldCurveName;
    }
}