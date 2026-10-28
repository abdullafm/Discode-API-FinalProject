using System.Security.Cryptography;

namespace Domain.Common
{
    public static class PublicIdGenerator
    {
        private const ulong Min = 100_000_000_000_000_000UL;
        private const ulong Range = 900_000_000_000_000_000UL;

        public static string New() => (Min + BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8)) % Range).ToString();
    }
}
