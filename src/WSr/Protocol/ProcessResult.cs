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
        public ProcessResult(
            DateTimeOffset timestamp, 
            string counterPart, 
            ResultType type)
        {
            TimeStamp = timestamp;
            CounterPart = counterPart;
            Type = type;
        }

        public DateTimeOffset TimeStamp { get; }

        public string CounterPart { get; }
        public ResultType Type { get; }

        public bool Equals(ProcessResult other)
        {
            return CounterPart.Equals(other.CounterPart) &&
                Type.Equals(other.Type);
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new []
            {
                $"{Type.ToString()}:",
                $"TimeStamp: {TimeStamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss:FFFFFFF")}",
                $"CounterPart: {CounterPart}"
            });
        }
    }
}