using System;
using System.Collections.Generic;

namespace WSr.Frame
{
    public interface IFrameBuilder<T>
    {
        bool Complete { get; }
        T Payload { get; }
        Func<byte, IFrameBuilder<T>> Next { get; }
    }
}