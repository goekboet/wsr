using System;

namespace WSr.Protocol
{
    public enum ResultType
    {
        UnSuccessfulOpeningHandshake,
        SuccessfulOpeningHandshake,
        TextMessageSent,
        CloseHandshakeFinished,
        CloseSocket,
        NoOp
    }

    public class ProcessResult : IEquatable<ProcessResult>
    {
        public ProcessResult(string counterPart, ResultType type)
        {
            CounterPart = counterPart;
            Type = type;
        }

        public string CounterPart { get; }
        public ResultType Type { get; }

        public bool Equals(ProcessResult other)
        {
            return CounterPart.Equals(other.CounterPart) &&
                Type.Equals(other.Type);
        }
    }
}