using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReferenceDataHub
{
    public record FinancialDimesnionRequest<TDim> : IRequest<TDim>;
}
