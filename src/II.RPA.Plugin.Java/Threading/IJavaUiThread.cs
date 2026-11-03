using System;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Java
{
    internal interface IJavaUiThread
    {
        bool CheckAccess();

        void Invoke(Action operation);

        TResult Invoke<TResult>(Func<TResult> operation);

        Task InvokeAsync(Action operation);

        Task<TResult> InvokeAsync<TResult>(Func<TResult> operation);

        Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> operation);
    }
}
