using System;
using System.Collections.Generic;

namespace WSr.Frame
{
    public class FirstByteIsLength : IFrameBuilder<IList<byte>>
    {
        public static IFrameBuilder<IList<byte>> Init => new FirstByteIsLength();
        public bool Complete => _needs == 0;
        public IList<byte> Payload { get; } = new List<byte>();
        public Func<byte, IFrameBuilder<IList<byte>>> Next { get; private set; }
        int _needs = -1;

        private IFrameBuilder<IList<byte>> ReadLength(byte b)
        {
            _needs = (int)b;
            Payload.Clear();
            Next = BuildPayload;
            return this;
        }
        private IFrameBuilder<IList<byte>> BuildPayload(byte b)
        {
            Payload.Add(b);
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