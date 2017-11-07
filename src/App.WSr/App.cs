using System;
using WSr;

namespace App.WSr
{
    public static class Apps
    {
        public static Func<IObservable<Message>, IObservable<Message>> Echo => m => m;
    }
}