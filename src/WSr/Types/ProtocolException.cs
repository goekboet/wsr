using System;

namespace WSr
{
    public class ProtocolException : Exception
    {
        public ushort Code { get; }
        public ProtocolException(string m, ushort c) : base(m) => 
            Code = c;
    }
}