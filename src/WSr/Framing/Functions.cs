using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;

using static WSr.Framing.Functions;
using static WSr.ListConstruction;
using static WSr.IntegersFromByteConverter;

namespace WSr.Framing
{
    public static class Functions
    {
        public static bool IsMasked(IEnumerable<byte> bitfield)
        {
            return bitfield.Skip(1).Select(b => (b & 0x80) != 0).Take(1).Single();
        }

        public static ulong InterpretLengthBytes(IEnumerable<byte> bytes)
        {
            if (BitConverter.IsLittleEndian)
                bytes = bytes.Reverse();

            if (bytes.Count() == 2)
                return (ulong)BitConverter.ToUInt16(bytes.ToArray(), 0);

            return BitConverter.ToUInt64(bytes.ToArray(), 0);
        }

        public static IEnumerable<byte> UnMask(IEnumerable<byte> mask, IEnumerable<byte> payload)
        {
            return payload.Zip(Forever(mask).SelectMany(x => x), (p, m) => (byte)(p ^ m));
        }
        
        public static Parse ToFrame(
            (bool masked, int bitfieldLength, IEnumerable<byte> frame) parse)
        {
            var bitfield = parse.frame.Take(2);

            int lenghtBytes = 0;
            if (parse.bitfieldLength == 126)
                lenghtBytes = 2;
            else if (parse.bitfieldLength == 127)
                lenghtBytes = 8;

            var length = parse.frame.Skip(2).Take(lenghtBytes);
            var mask = parse.masked
                ? parse.frame.Skip(2 + lenghtBytes).Take(4)
                : Enumerable.Empty<byte>();

            var payload = parse.frame.Skip(2 + lenghtBytes + (parse.masked ? 4 : 0));

            return new Parse(
                bitfield: bitfield,
                payload: parse.masked ? UnMask(mask, payload) : payload
            );
        }

        public static Frame IsValid(Parse frame)
        {
            if (frame.OpCodeLengthLessThan126())
                return Bad.ProtocolError("Opcode payloadlength must be < 125");
            if (frame.ReservedBitsSet())
                return Bad.ProtocolError("RSV-bit is set");
            if (frame.BadOpcode())
                return Bad.ProtocolError("Not a valid opcode");
            if (frame.ControlframeNotFinal())
                return Bad.ProtocolError("Control-frame must be final");

            return frame;
        }

        public static IObservable<IEnumerable<IEnumerable<T>>> Chop<T>(
            this IObservable<T> source,
            T[] lineterminator,
            Func<IEnumerable<T>, bool> eof)
        {
            return Observable.Create<IEnumerable<IEnumerable<T>>>(o =>
            {
                var file = new List<IEnumerable<T>>();
                var line = new List<T>();
                var skip = 0;

                return source.Buffer(lineterminator.Length, 1).Subscribe(bs =>
                {
                    try
                    {
                        if (skip > 0)
                        {
                            --skip;
                        }
                        else if (lineterminator.SequenceEqual(bs))
                        {
                            if (eof(line))
                            {
                                o.OnNext(file.ToArray());
                                file.Clear();
                            }
                            else
                            {
                                file.Add(line.ToArray());
                            }
                            line.Clear();
                            skip = lineterminator.Length - 1;
                        }
                        else
                        {
                            line.Add(bs.First());
                        }
                    }
                    catch (Exception e)
                    {
                        o.OnError(e);
                    }
                },
                o.OnError,
                o.OnCompleted);
            });
        }

        public static IObservable<IEnumerable<string>> ChopUpgradeRequest(
            this IObservable<byte> bytes) => bytes
                .Chop(new byte[] { 0x0d, 0x0a }, bs => bs.Count() == 0)
                .Select(x => x.Select(y => Encoding.ASCII.GetString(y.ToArray())));

        public static IMessage ToHandshakeMessage(
            string origin,
            IEnumerable<string> upgrade)
        {
            string getUrl(IEnumerable<string> u) => u.First();

            IEnumerable<string> getHeaders(IEnumerable<string> u) => u.Skip(1);

            string url;
            IDictionary<string, string> headers;
            try
            {
                url = Regex.Matches(getUrl(upgrade), parseRequestLine)[0].Groups[1].Value;
            }
            catch (FormatException)
            {
                return new BadUpgradeRequest(origin, UpgradeFail.MalformedRequestLine);
            }
            try
            {
                headers = getHeaders(upgrade)
                    .Select(l =>
                    {
                        var line = Regex.Matches(l, parseHeaderLine)[0].Groups;
                        return new KeyValuePair<string, string>(line[1].Value, line[2].Value);
                    }).ToDictionary(x => x.Key, x => x.Value);
            }
            catch (FormatException)
            {
                return new BadUpgradeRequest(origin, UpgradeFail.MalformedHeaderLine);
            }

            return Validate(headers)
                ? new UpgradeRequest(origin, url, headers) as IMessage
                : new BadUpgradeRequest(origin, UpgradeFail.MissRequiredHeader);
        }

        private static string parseRequestLine = @"^GET\s(/\S*)\sHTTP/1\.1$";
        private static string parseHeaderLine = @"^(\S*):\s(\S*)$";
        private static HashSet<string> RequiredHeaders = new HashSet<string>(new[]
        {
            "Host",
            "Upgrade",
            "Connection",
            "Sec-WebSocket-Key",
            "Sec-WebSocket-Version"
        });

        public static bool Validate(IDictionary<string, string> headers)
        {
            return RequiredHeaders.IsSubsetOf(new HashSet<string>(headers.Keys));
        }
    }
}