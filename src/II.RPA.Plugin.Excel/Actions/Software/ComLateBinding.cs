using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace II.RPA.Plugin.Excel.Actions.Software
{
    public static class ComLateBinding
    {
        private const int MaxRetryCount = 30;
        private const int RetryDelayMilliseconds = 100;
        private const int RpcServerCallRetryLater = unchecked((int)0x8001010A);
        private const int RpcCallRejected = unchecked((int)0x80010001);
        private const int VbaIgnore = unchecked((int)0x800AC472);

        public static object GetProp(object obj, string name, params object[] args)
        {
            if (obj == null)
            {
                return null;
            }

            return InvokeWithRetry(() => obj.GetType().InvokeMember(name, BindingFlags.GetProperty, null, obj, args));
        }

        public static void SetProp(object obj, string name, object value)
        {
            if (obj == null)
            {
                return;
            }

            InvokeWithRetry<object>(() =>
            {
                obj.GetType().InvokeMember(name, BindingFlags.SetProperty, null, obj, new object[] { value });
                return null;
            });
        }

        public static void SetPropIndexed(object obj, string name, object[] indexArgs, object value)
        {
            if (obj == null)
            {
                return;
            }

            object[] totalArgs = new object[indexArgs.Length + 1];
            Array.Copy(indexArgs, totalArgs, indexArgs.Length);
            totalArgs[totalArgs.Length - 1] = value;
            InvokeWithRetry<object>(() =>
            {
                obj.GetType().InvokeMember(name, BindingFlags.SetProperty, null, obj, totalArgs);
                return null;
            });
        }

        public static object Invoke(object obj, string name, params object[] args)
        {
            if (obj == null)
            {
                return null;
            }

            return InvokeWithRetry(() => obj.GetType().InvokeMember(name, BindingFlags.InvokeMethod, null, obj, args));
        }

        public static object InvokeNamed(object obj, string name, object[] args, string[] namedParameters)
        {
            if (obj == null)
            {
                return null;
            }

            return InvokeWithRetry(() =>
                obj.GetType().InvokeMember(
                    name,
                    BindingFlags.InvokeMethod,
                    null,
                    obj,
                    args,
                    null,
                    CultureInfo.InvariantCulture,
                    namedParameters));
        }

        public static bool IsRetryableComException(Exception ex)
        {
            COMException comException = GetComException(ex);
            if (comException == null)
            {
                return false;
            }

            return comException.ErrorCode == RpcServerCallRetryLater
                || comException.ErrorCode == RpcCallRejected
                || comException.ErrorCode == VbaIgnore;
        }

        private static T InvokeWithRetry<T>(Func<T> action)
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    return action();
                }
                catch (Exception ex) when (IsRetryableComException(ex) && attempt < MaxRetryCount)
                {
                    Thread.Sleep(RetryDelayMilliseconds);
                }
            }
        }

        private static COMException GetComException(Exception ex)
        {
            while (ex is TargetInvocationException && ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            return ex as COMException;
        }

        public static object GetItem(object obj, object index)
        {
            return GetProp(obj, "Item", index);
        }

        public static object GetItem(object obj, object row, object column)
        {
            return GetProp(obj, "Item", row, column);
        }

        public static int ToInt(object value)
        {
            return value == null ? 0 : Convert.ToInt32(value);
        }

        public static double ToDouble(object value)
        {
            return value == null ? 0d : Convert.ToDouble(value);
        }

        public static bool ToBool(object value)
        {
            return value != null && Convert.ToBoolean(value);
        }

        public static string ToText(object value)
        {
            return value?.ToString() ?? string.Empty;
        }

        public static IEnumerable ToEnumerable(object value)
        {
            try
            {
                var enumerable = value as IEnumerable;
                if (enumerable != null)
                {
                    return enumerable;
                }
            }
            catch (Exception)
            {

            }

            var items = new ArrayList();
            int count = ToInt(GetProp(value, "Count"));
            for (int index = 1; index <= count; index++)
            {
                items.Add(GetItem(value, index));
            }

            return items;
        }
    }
}
