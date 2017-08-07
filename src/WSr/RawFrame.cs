using System;
using System.Collections.Generic;
using System.Linq;

namespace WSr
{
    /// <summary>
    /// Represents a websocket frame according to specification.
    /// </summary>
    public class RawFrame : IEquatable<RawFrame>
    {
        public static RawFrame Empty { get; } =
            new RawFrame(
                bitfield: new byte[2],
                length: new byte[8],
                mask: new byte[4],
                payload: new byte[0]);

        public RawFrame(
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
        public bool Equals(RawFrame other)
        {
            return this.Payload.SequenceEqual(other.Payload) &&
                this.Mask.SequenceEqual(other.Mask) &&
                this.Length.SequenceEqual(other.Length) &&
                this.Bitfield.SequenceEqual(other.Bitfield);
        }

        public override bool Equals(object obj)
        {
            var other = obj as RawFrame;

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
}