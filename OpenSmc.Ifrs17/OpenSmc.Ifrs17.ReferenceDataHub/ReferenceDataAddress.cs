using OpenSmc.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

public record ReferenceDataAddress(object Host) : IHostedAddress;

public record ReferenceDataImportAddress(object Host) : IHostedAddress;

