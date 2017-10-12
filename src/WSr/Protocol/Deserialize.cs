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

        public static IObservable<Message> DeserializeHandshake(
            this IObservable<byte> bytes
        ) => bytes
                .ChopUpgradeRequest()
                .Select(ParseHandshake)
                .Select(x => x.Map(AcceptKey))
                .Select(AcceptHandshake)
                ;
        
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
                .DeserializeHandshake()
                .Take(1)
                ;

            var frames = incoming
                .ParseWSFrame()
                .Select(ToFrame)
                .Select(p => p.Map(IsValid))
                .PingPongWithFrames()
                // .PingPongWithFrames(TimeSpan.FromSeconds(10), l => AddContext("Latency", ctx)(l.ToString()))
                .Select(x => x.Map(CloseHandshake))
                .DecodeUtf8Payload()
                .Defrag()
                .Select(ToMessage)
                ;

            return handshake.Concat(frames);
        }
    }
}