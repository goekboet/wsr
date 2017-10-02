using System;
using System.Reactive.Linq;

namespace WSr.Messaging
{
    public static class ControlCodesFunctions
    {
        public static IObservable<Message> Controlcodes(
            Message incoming)
        {
            if (incoming is OpcodeMessage m)
            {
                if (m.Opcode == OpCode.Ping)
                    return Observable.Return(new OpcodeMessage(OpCode.Pong, m.Buffer));
                if (m.Opcode == OpCode.Close)
                    return Observable.Return(m).Concat<Message>(Observable.Return(Eof.Message));
            }
            
            return Observable.Return(incoming);
        }
    }
}