using System;
using System.Collections.Generic;
using System.Linq;

namespace WSr.Frame
{
    /// <summary>
    /// A class that implements IFramereaderState<T> will take a byte and 
    /// produce a new state until it represent a completed T.
    /// </summary>
    public interface IFrameReaderState<T>
    {
        /// <summary>
        /// Indicates if the state represents a completed T or not. 
        /// </summary>
        /// <returns>True if the state is complete, false otherwise</returns>
        bool Complete { get; }
        /// <summary>
        /// Returns what the state has built up so far
        /// </summary>
        /// <returns></returns>
        T Payload { get; }
        /// <summary>
        /// Take a byte and return a state that represents that bytes contribution towards a completed state.
        /// </summary>
        /// <returns>The new state</returns>
        Func<byte, IFrameReaderState<T>> Next { get; }
    }

    /// <summary>
    /// Represents a websocket frame according to specification.
    /// </summary>
    public class WebSocketFrame : IEquatable<WebSocketFrame>
    {
        public static WebSocketFrame Empty { get; } =
            new WebSocketFrame(
                bitfield: new byte[2],
                length: new byte[8],
                mask: new byte[4],
                payload: new byte[0]);

        public WebSocketFrame(
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
        public bool Equals(WebSocketFrame other)
        {
            return this.Payload.SequenceEqual(other.Payload) &&
                this.Mask.SequenceEqual(other.Mask) &&
                this.Length.SequenceEqual(other.Length) &&
                this.Bitfield.SequenceEqual(other.Bitfield);
        }

        public override bool Equals(object obj)
        {
            var other = obj as WebSocketFrame;

            return obj == null ? false : this.Equals(other);
        }
        /// <summary>
        /// This class is not intended to work with hashcodes.The purpose of this implementation is
        /// to get rid of compiler warnings regarding it missing eventhough we override object.Equals
        /// </summary>
        /// <returns>Always 0</returns>
        public override int GetHashCode() => 0;
    }
}