using System;

namespace II.RPA.Plugin.SAP.Extensions
{
    /// <summary>
    /// SAP COM 类型与 C# 强类型之间的安全转换扩展。
    /// </summary>
    public static class SapTypeExtensions
    {
        /// <summary>
        /// 将 C# bool 转换为 SAP COM 期望的 sbyte 值（0 或 1）。
        /// </summary>
        public static sbyte ToSapBool(this bool value)
        {
            return value ? (sbyte)1 : (sbyte)0;
        }

        /// <summary>
        /// 将 SAP COM sbyte 值（非 0 即为 true）转换为 C# bool。
        /// </summary>
        public static bool ToCSharpBool(this sbyte value)
        {
            return value != 0;
        }
    }
}
