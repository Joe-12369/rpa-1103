using Accessibility;
using FlaUI.Core.Patterns;
using System.Reflection;

namespace II.RPA.Plugin.Windows
{
    internal static class LegacyIAccessiblePatternExtensions
    {
        public static IAccessible GetIAccessible(this ILegacyIAccessiblePattern pattern)
        {
            var nativePattern = pattern?.GetType().GetProperty("NativePattern", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(pattern);
            return nativePattern?.GetType().GetMethod("GetIAccessible", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(nativePattern, null) as IAccessible;
        }
    }
}
