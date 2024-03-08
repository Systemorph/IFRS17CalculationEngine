using OpenSmc.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSmc.Ifrs17.ImportHubs
{
    public record IfrsImportAddress(int Year, object Host) : IHostedAddress;
    
}
