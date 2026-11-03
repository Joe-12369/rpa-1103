using System.Threading;

namespace II.RPA.Core.GRPCService.Util
{
    internal static class MessageIdGenerator
    {
        private static int s_NextMessageId;

        public static int NextMessageId()
        {
            var next = Interlocked.Increment(ref s_NextMessageId);
            if (next <= 0)
            {
                Interlocked.Exchange(ref s_NextMessageId, 1);
                return 1;
            }

            return next;
        }
    }
}
