using System;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Test
{
    internal sealed class MethodInfo
    {
        private readonly System.Reflection.MethodInfo method;

        private MethodInfo(System.Reflection.MethodInfo method)
        {
            this.method = method ?? throw new ArgumentNullException(nameof(method));
        }

        public object Invoke(object target, object[] parameters)
        {
            if (typeof(Task<string>).IsAssignableFrom(method.ReturnType))
            {
                return Task.Run(async () =>
                {
                    var task = (Task<string>)method.Invoke(target, parameters);
                    return await task.ConfigureAwait(false);
                }).ConfigureAwait(false).GetAwaiter().GetResult();
            }

            if (typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                Task.Run(async () =>
                {
                    var task = (Task)method.Invoke(target, parameters);
                    await task.ConfigureAwait(false);
                }).ConfigureAwait(false).GetAwaiter().GetResult();
                return null;
            }

            return method.Invoke(target, parameters);
        }

        public static implicit operator MethodInfo(System.Reflection.MethodInfo method)
        {
            return method == null ? null : new MethodInfo(method);
        }
    }
}
