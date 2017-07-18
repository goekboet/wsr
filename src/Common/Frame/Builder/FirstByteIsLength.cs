using System;
using System.Collections.Generic;

namespace WSr.Frame
{
    public class FirstByteIsLength : IParserState<IList<byte>>
    {
        public static IParserState<IList<byte>> Init => new FirstByteIsLength();
        public bool Complete => _needs == 0;
        public IList<byte> Payload { get; } = new List<byte>();
        public Func<byte, IParserState<IList<byte>>> Next { get; private set; }
        int _needs = -1;

        private IParserState<IList<byte>> ReadLength(byte b)
        {
            _needs = (int)b;
            Payload.Clear();
            Next = BuildPayload;
            return this;
        }
        private IParserState<IList<byte>> BuildPayload(byte b)
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