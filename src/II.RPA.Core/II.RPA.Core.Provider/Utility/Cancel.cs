using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.Utility
{
    public static class CancellationTokenExtensions
    {
        public static Task AsTask(this CancellationToken @this)
        {
            var tcs = new TaskCompletionSource<Unit>();
            @this.Register(() => tcs.SetResult(default(Unit)));
            return tcs.Task;
        }
    }
}
