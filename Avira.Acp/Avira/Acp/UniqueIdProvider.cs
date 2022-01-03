using System.Threading;

namespace Avira.Acp
{
    public static class UniqueIdProvider
    {
        private static long lastUsedUniqueId;

        public static string Get()
        {
            return Interlocked.Increment(ref lastUsedUniqueId).ToString();
        }

        internal static string GetLast()
        {
            return lastUsedUniqueId.ToString();
        }
    }
}