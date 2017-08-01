using System;
using System.Collections.Generic;

namespace WSr.Frame
{
    /// <summary>
    /// This class is a proof of concept of the FrameReaderstate approach 
    /// to parse bytes comming off the connected socket.
    /// </summary>
    public class FirstByteIsLength : IFrameReaderState<IList<byte>>
    {
        public static IFrameReaderState<IList<byte>> Init => new FirstByteIsLength();
        public bool Complete => _needs == 0;
        public IList<byte> Reading { get; } = new List<byte>();
        public Func<byte, IFrameReaderState<IList<byte>>> Next { get; private set; }
        int _needs = -1;

        private IFrameReaderState<IList<byte>> ReadLength(byte b)
        {
            _needs = (int)b;
            Reading.Clear();
            Next = BuildPayload;
            return this;
        }
        private IFrameReaderState<IList<byte>> BuildPayload(byte b)
        {
            Reading.Add(b);
            _needs--;
            if (_needs == 0) Next = ReadLength;
            return this;
        }

        private FirstByteIsLength()
        {
            Next = ReadLength;
        }
    }
}