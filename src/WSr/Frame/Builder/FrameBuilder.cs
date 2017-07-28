using System;

using static WSr.Frame.Functions;

namespace WSr.Frame
{
    public class FrameBuilder : IFrameReaderState<RawFrame>
    {
        public static IFrameReaderState<RawFrame> Init => new FrameBuilder();
        
        public bool Complete { get; private set; }

        public RawFrame Payload => new RawFrame(
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
            _lengthbytes = new byte[8];
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
                        _lengthbytes[0] = (byte)l;
                        if (masked)
                            Next = ReadMaskBytes();
                        else
                            Next = ReadPayloadBytes((ulong)l);
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
            var read = MakeReader(_lengthbytes, count - 1);

            return b =>
            {
                var hasNext = read(b);
                if (!hasNext)
                {
                    if (masked)
                        Next = ReadMaskBytes();
                    else
                        Next = ReadPayloadBytes(BitConverter.ToUInt64(_lengthbytes, 0));
                }
                return this;
            };
        }

        private Func<byte, IFrameReaderState<RawFrame>> ReadMaskBytes()
        {
            var read = MakeReader(_maskbytes);

            return b =>
            {
                var hasNext = read(b);
                if (!hasNext) Next = ReadPayloadBytes(BitConverter.ToUInt64(_lengthbytes, 0));

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