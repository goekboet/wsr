using System;
using System.Collections.Generic;
using System.Linq;
using WSr.Framing;

namespace WSr
{
    public abstract class Frame
    {
        public abstract string Origin { get; }
    }

    /// <summary>
    /// Represents a websocket frame according to specification.
    /// </summary>
    public class ParsedFrame : Frame, IEquatable<ParsedFrame>
    {
        public ParsedFrame(
            string origin,
            byte[] bitfield,
            byte[] length,
            byte[] mask,
            byte[] payload)
        {
            Origin = origin;
            Bitfield = bitfield.Clone() as IEnumerable<byte>;
            Length = length.Clone() as IEnumerable<byte>;
            Mask = mask.Clone() as IEnumerable<byte>;
            Payload = payload.Clone() as IEnumerable<byte>;
        }

        public override string Origin { get; }

        public IEnumerable<byte> Bitfield { get; }

        public IEnumerable<byte> Length { get; }

        public IEnumerable<byte> Mask { get; }

        public IEnumerable<byte> Payload { get; }

        /// <summary>
        /// Two instances are considered equal if each sequence are the same.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ParsedFrame other)
        {
            if (other == null) return false;

            return this.Payload.SequenceEqual(other.Payload) &&
                this.Mask.SequenceEqual(other.Mask) &&
                this.Length.SequenceEqual(other.Length) &&
                this.Bitfield.SequenceEqual(other.Bitfield);
        }

        public override bool Equals(object obj)
        {
            var other = obj as ParsedFrame;

            return obj == null ? false : this.Equals(other);
        }

        /// <summary>
        /// This class is not intended to work with hashcodes.The purpose of this implementation is
        /// to get rid of compiler warnings regarding it missing eventhough we override object. The Message 
        /// class is better to group by.
        /// </summary>
        /// <returns>Always 0</returns>
        public override int GetHashCode() => 0;
    }

    public class Defragmented : Frame
    {
        public Defragmented(ParsedFrame parse) 
            : this(parse.Origin, parse.GetOpCode(), parse.UnMaskedPayload()) 
        {
        }
        
        public Defragmented(
            string origin,
            OpCode opCode,
            IEnumerable<byte> payload)
        {
            Origin = origin;
            OpCode = opCode;
            Payload = payload;
        }

        public OpCode OpCode { get; }
        public IEnumerable<byte> Payload { get; }
        public override string Origin { get; }

        public override bool Equals(object obj)
        {
            if (obj is Defragmented d)
            {
                return Origin.Equals(d.Origin) &&
                       OpCode.Equals(d.OpCode) &&
                       Payload.SequenceEqual(d.Payload);
            }

            return false;
        }  

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 * Origin.GetHashCode();
                hash = hash * 31 * OpCode.GetHashCode();
                hash = hash * 31 * Payload.Count();

                return hash;
            }
        }      
    }

    public class BadFrame : Frame
    {
        public static BadFrame ParserError => new BadFrame("", "Parsererror");

        public static BadFrame MessageMapperError(string e) => new BadFrame("", e);
        public BadFrame(
            string origin,
            string reason)
        {
            Origin = origin;
            Reason = reason;
        }
        public override string Origin { get; }
        public string Reason { get; }

        public override bool Equals(object obj)
        {
            if (obj is BadFrame b)
            {
                return Origin.Equals(b.Origin) &&
                       Reason.Equals(b.Reason);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 * Origin.GetHashCode();
                hash = hash * 31 * Reason.GetHashCode();

                return hash;
            }
        }
    }
}