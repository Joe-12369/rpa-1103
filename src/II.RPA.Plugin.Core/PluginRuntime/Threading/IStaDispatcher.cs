using System;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.Threading
{
    public interface IStaDispatcher
    {
        Task InvokeAsync(Action operation);
        Task<TResult> InvokeAsync<TResult>(Func<TResult> operation);
        Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> operation);
    }
}
