using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.Utility
{
    static class CompareExchangeEnumImpl<T>
    {
        public delegate T dImpl(ref T location, T value, T comparand);
        public static readonly dImpl Impl = CreateCompareExchangeImpl();

        static dImpl CreateCompareExchangeImpl()
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(T), new[] { typeof(T).MakeByRefType(), typeof(T), typeof(T) });
            var ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(
                OpCodes.Call,
                typeof(Interlocked).GetMethod(
                    "CompareExchange",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { underlyingType.MakeByRefType(), underlyingType, underlyingType },
                    null));
            ilGenerator.Emit(OpCodes.Ret);
            return (dImpl)dynamicMethod.CreateDelegate(typeof(dImpl));
        }
    }

    public static class InterlockedEx
    {
        public static T CompareExchangeEnum<T>(ref T location, T value, T comparand)
        {
            return CompareExchangeEnumImpl<T>.Impl(ref location, value, comparand);
        }
    }
}
