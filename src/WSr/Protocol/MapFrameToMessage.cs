using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace WSr.Protocol
{
    public static class MapFrameToMessageFunctions
    {
        public static Func<Parse<string, HandshakeParse>, Message> AcceptHandshake => p =>
        {
            (var error, var data) = p;

            return string.IsNullOrEmpty(error)
                ? new UpgradeRequest(data) as Message
                : new BadUpgradeRequest(error);
        };

        public static Func<Parse<FailedFrame, Frame>, IObservable<Message>> ToMessage =>
            frame =>
        {
            (var e, var f) = frame;

            if (frame.IsError) 
                return Observable.Return(ToOpcodeMessage(e))
                    .Concat(Observable.Return(OpcodeMessage.Empty));
            else if (f.GetOpCode() == OpCode.Close)
                return Observable.Return(ToOpcodeMessage(f))
                    .Concat(Observable.Return(OpcodeMessage.Empty));
            else
                return Observable.Return(ToOpcodeMessage(f));
        };

        private static Message ToOpcodeMessage(FailedFrame f) => new OpcodeMessage(OpCode.Close, f.Payload);
        private static Message ToOpcodeMessage(Frame p) => new OpcodeMessage(p.GetOpCode(), p.Payload);
    }
}