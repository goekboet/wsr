using System;
using System.Collections.Generic;
using System.Linq;
using WSr.Framing;

using static WSr.IntegersFromByteConverter;

namespace WSr
{
    public abstract class Frame
    {
    }

    public interface IBitfield
    {
        IEnumerable<byte> Bits { get; }
    }
}