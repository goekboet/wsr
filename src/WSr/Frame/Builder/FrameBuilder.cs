using System;
using System.Collections.Generic;
using System.Linq;
using static WSr.Frame.Functions;

namespace WSr.Frame
{
    public class FrameBuilder : IFrameReaderState<RawFrame>
    {
        public static IFrameReaderState<RawFrame> Init => new FrameBuilder();
        
        public bool Complete { get; private set; }

        public RawFrame Reading => new RawFrame(
            bitfield: _bitfield,
            length: _lengthbytes,
            mask: _maskbytes,
            payload: _payload
        );

        public Func<byte, IFrameReaderState<RawFrame>> Next { get; private set; }

        public FrameBuilder()
        {
            Initialize();
            Next = ReadBitfield(false);
        }

        private byte[] _bitfield = new byte[2];
        private byte[] _lengthbytes;
        private byte[] _maskbytes;
        private byte[] _payload;

        private void Initialize()
        {
            _lengthbytes = new byte[0];
            _maskbytes = new byte[4];
            _payload = new byte[0];
            Complete = false;
        }

        private Func<byte, IFrameReaderState<RawFrame>> ReadBitfield(bool c)
        {
            Complete = c;
            var read = MakeReader(_bitfield);

            return b =>
            {
                if (Complete) Initialize();

                var hasNext = read(b);
                if (!hasNext)
                {
                    var l = BitFieldLength(_bitfield);
                    var masked = IsMasked(_bitfield);
                    if (l < 126)
                    {
                        if (masked)
                            Next = ReadMaskBytes((ulong)l);
                        else
                            Next = l == 0 ? ReadBitfield(true) : ReadPayloadBytes((ulong)l);
                    }
                    else if (l == 126)
                        Next = ReadLengthBytes(2, masked);
                    else
                        Next = ReadLengthBytes(8, masked);
                }

                return this;
            };
        }
        private Func<byte, IFrameReaderState<RawFrame>> ReadLengthBytes(int count, bool masked)
        {
            _lengthbytes = new byte[count];
            var read = MakeReader(_lengthbytes, count - 1);

            return b =>
            {
                var hasNext = read(b);
                if (!hasNext)
                {
                    if (masked)
                        Next = ReadMaskBytes(InterpretLengthBytes(_lengthbytes));
                    else
                        Next = ReadPayloadBytes(InterpretLengthBytes(_lengthbytes));
                }
                return this;
            };
        }

        private Func<byte, IFrameReaderState<RawFrame>> ReadMaskBytes(ulong payloadLength)
        {
            var read = MakeReader(_maskbytes);

            return b =>
            {
                var hasNext = read(b);
                if (!hasNext) 
                    Next = BitFieldLength(_bitfield) == 0 
                        ? ReadBitfield(true) 
                        : ReadPayloadBytes(payloadLength);

                return this;
            };
        }

        private Func<byte, IFrameReaderState<RawFrame>> ReadPayloadBytes(ulong count)
        {
            _payload = new byte[count];
            var read = MakeReader(_payload);

            return b =>
            {
                var hasNext = read(b);
                if (!hasNext)
                {
                    Next = ReadBitfield(true);
                }

                return this;
            };
        }
    }
}