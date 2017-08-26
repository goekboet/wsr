using System;
using System.Collections.Generic;
using WSr.Messaging;

namespace WSr
{
    public interface ICommand
    {
        string Origin { get; }
    }

    public class IOCommand : ICommand, IEquatable<ICommand>
    {
        public IOCommand(
            IMessage message,
            IEnumerable<byte> outbound)
        {
            OutBound = outbound;
            Message = message;
        }

        public IMessage Message { get; }
        public IEnumerable<byte> OutBound { get; }

        public string Origin => Message.Origin;

        public bool Equals(ICommand other)
        {
            if (other == null) return false;

            return Message.Origin.Equals(other.Origin);
        }

        public override bool Equals(object obj) => Equals(obj as IOCommand);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 * Origin.GetHashCode();

                return hash;
            }
        }
    }

    public class EOF : ICommand, IEquatable<EOF>
    {
        public EOF(string origin)
        {
            Origin = origin;
        }
        public string Origin { get; }

        public bool Equals(EOF other)
        {
            if (other == null) return false;

            return Origin.Equals(other.Origin);
        }

        public override bool Equals(object obj) => Equals(obj as EOF);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 * Origin.GetHashCode();

                return hash;
            }
        }
    }
}