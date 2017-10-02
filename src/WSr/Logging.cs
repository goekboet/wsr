using System;

namespace WSr
{
    public static class LogFunctions
    {
        public static Action<string> AddContext(string ctx, Action<string> log) => 
            s => log($"{ctx}/{s}");

        public static Action<string> Timestamp(Action<string> l, DateTimeOffset t) => 
            AddContext(t.ToString(), l);
    }
}