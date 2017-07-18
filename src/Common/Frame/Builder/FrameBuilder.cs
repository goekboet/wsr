using System;
using System.Collections.Generic;

namespace WSr.Frame
{
    public interface IParserState<T>
    {
        bool Complete { get; }
        T Payload { get; }
        Func<byte, IParserState<T>> Next { get; }
    }
}