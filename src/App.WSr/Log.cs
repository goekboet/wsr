using System;
using System.IO;

namespace App.WSr
{
    public static class Loggers
    {
        public static Action<string> StdOut => l => Console.WriteLine(l);

        static Action<string> FileLog(string path) => l =>
        {
            File.AppendAllText(path, l + Environment.NewLine);
        };
    }
}