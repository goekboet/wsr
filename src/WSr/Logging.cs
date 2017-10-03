using System;

namespace WSr
{
    public static class LogFunctions
    {
        public static string TenDigitsTicks(DateTimeOffset t) => (t.Ticks % 1000000000).ToString("D10"); 
        public static Action<string> AddContext(string ctx, Action<string> log) => 
            s => log($"{ctx}/{s}");

        public static Action<string> Timestamp(Action<string> l, DateTimeOffset t) => 
            AddContext(TenDigitsTicks(t), l);
    }
}