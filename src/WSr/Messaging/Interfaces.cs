using System.Collections.Generic;

namespace WSr.Messaging
{
    public abstract class Message
    {
        public abstract IEnumerable<string> To { get; }

        public abstract bool IsText { get; }

        public abstract byte[] Payload { get; }
    }
}