using OpenSmc.Messaging;

using Scenario = OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions.Scenario;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

/*************/
/*  Request  */
/*************/
public record GetScenarioRequest : IRequest<ScenarioData>;
public record GetDimensionsRequest : IRequest<Dimensions>;

/**************/
/*  Response  */
/**************/
public record ScenarioData(Scenario[] Scenario);
public record Dimensions(LineOfBusiness[] LineOfBusinesses, AmountType[] AmountTypes);


