using System.Security.Cryptography;

namespace WSr
{
    public static class Algorithms
    {
        public static SHA1 SHA1 => SHA1.Create();
    }
}