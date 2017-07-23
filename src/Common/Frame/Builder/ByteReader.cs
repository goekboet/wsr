using System;
using System.Collections.Generic;
using System.Linq;
using WSr.Frame;

namespace WSr.ByteReaderState.ByteSequenceReader
{
    public static class Functions
    {
        public static IParserState<ByteSequences> Init => new ByteReaderState();
        public static int BitFieldLength(byte[] bitfield)
        {
            return bitfield[1] & 0x7f;
        }

        public static bool IsMasked(byte[] bitfield)
        {
            return (bitfield[1] & 0x80) != 0;
        }
    }

    public class ByteSequences : IEquatable<ByteSequences>
    {
        public static ByteSequences Empty { get; } = 
            new ByteSequences(
                bitfield: new byte[2],
                length: new byte[8],
                mask: new byte[4],
                payload: new byte[0]);

        public ByteSequences(
            byte[] bitfield,
            byte[] length,
            byte[] mask,
            byte[] payload)
        {
            Bitfield = bitfield.Clone() as IEnumerable<byte>;
            Length = length.Clone() as IEnumerable<byte>;
            Mask = mask.Clone() as IEnumerable<byte>;
            Payload = payload.Clone() as IEnumerable<byte>;
        }

        public IEnumerable<byte> Bitfield { get; }

        public IEnumerable<byte> Length { get; } 

        public IEnumerable<byte> Mask { get; }

        public IEnumerable<byte> Payload { get; }

        /// <summary>
        /// Two instances are considered equal if each sequence are the same.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ByteSequences other)
        {
            return this.Payload.SequenceEqual(other.Payload) &&
                this.Mask.SequenceEqual(other.Mask) &&
                this.Length.SequenceEqual(other.Length) &&
                this.Bitfield.SequenceEqual(other.Bitfield);
        }

        public override bool Equals(object obj)
        {
            var other = obj as ByteSequences;

            return obj == null ? false : this.Equals(other);
        }
        /// <summary>
        /// This class is not intended to work with hashcodes.The purpose of this implementation is
        /// to get rid of compiler warnings regarding it missing eventhough we override object.Equals
        /// </summary>
        /// <returns>Always 0</returns>
        public override int GetHashCode() => 0;
    }

    public class ByteReaderState : IParserState<ByteSequences>
    {
        public bool Complete {get; private set;}

        public ByteSequences Payload => new ByteSequences(
            bitfield: _bitfield,
            length: _lengthbytes,
            mask: _maskbytes,
            payload: _payload
        );

        public Func<byte, IParserState<ByteSequences>> Next { get; private set;}

        public ByteReaderState()
        {
            Initialize();
            Next = ReadBitfield(false);
        }

        private byte[] _bitfield;
        private byte[] _lengthbytes;
        private byte[] _maskbytes;
        private byte[] _payload;

        private void Initialize()
        {
            _bitfield = new byte[2];
            _lengthbytes = new byte[8];
            _maskbytes = new byte[4];
            _payload = new byte[0];
        }

        private Func<byte, IParserState<ByteSequences>> ReadBitfield(bool c)
        {
            Complete = c;
            var read = Parse.MakeReader(_bitfield);

            return b =>
            {
                Complete = false;

                var hasNext = read(b);
                if (!hasNext)
                {
                    var l = Functions.BitFieldLength(_bitfield);
                    var masked = Functions.IsMasked(_bitfield);
                    if (l < 126)
                    {
                        _lengthbytes[0] = (byte)l;
                        if(masked)
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
        private Func<byte, IParserState<ByteSequences>> ReadLengthBytes(int count, bool masked)
        {
            var read = Parse.MakeReader(_lengthbytes, count - 1);

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

        private Func<byte, IParserState<ByteSequences>> ReadMaskBytes()
        {
            var read = Parse.MakeReader(_maskbytes);

            return b =>
            {
                var hasNext = read(b);
                if (!hasNext) Next = ReadPayloadBytes(BitConverter.ToUInt64(_lengthbytes, 0));

                return this;
            };
        }

        private Func<byte, IParserState<ByteSequences>> ReadPayloadBytes(ulong count)
        {
            _payload = new byte[count];
            var read = Parse.MakeReader(_payload);

            return b =>
            {
                var hasNext = read(b);
                if(!hasNext)
                {
                    Next = ReadBitfield(true);
                }

                return this;
            };
        }
    }
}