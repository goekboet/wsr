using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using static WSr.Protocol.Functions;
using static WSr.Protocol.HandshakeFunctions;
using static WSr.Protocol.MapFrameToMessageFunctions;
using static WSr.IntegersFromByteConverter;
using static WSr.LogFunctions;
using static WSr.Protocol.CloseHandshakeFunctions;

namespace WSr.Protocol
{
    public static class DeserializeFunctions
    {
        private const string Context = "Deserialize";
        
        public static IObservable<Message> Deserialize(
            this IObservable<byte> bytes,
            IScheduler s,
            Action<string> log)
        {
            var ctx = AddContext(Context, log);

            var incoming = bytes
                .Publish()
                .RefCount()
                ;

            var handshake = incoming
                .ChopUpgradeRequest()
                .Take(1)
                .Select(ParseHandshake)
                .Select(AcceptKey)
                .Select(AcceptHandshake)
                ;

            var frames = incoming
                .ParseWSFrame()
                .Select(ToFrame)
                .Select(IsValid)
                .Select(CloseHandshake)
                .DecodeUtf8Payload()
                .Defrag()
                .Select(ToMessage)
                ;

            return handshake.Concat(frames);
        }
    }
}