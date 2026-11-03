using System;

namespace II.RPA.Plugin.Core.CompareMode
{
    public static class CormpareFactory
    {
        public static bool IsDecimalRule(string pattern)
        {
            if (ConstFields.PATTERN_BIGGER == pattern || ConstFields.PATTERN_SMALLER == pattern)
            {
                return true;
            }
            return false;
        }

        public static ICompareMode<string> GenerateStringMode(string pattern)
        {
            pattern = pattern ?? "";
            if (pattern.Equals(ConstFields.PATTERN_BEGIN, StringComparison.OrdinalIgnoreCase))
            {
                return new StartwithMode();
            }
            else if (pattern.Equals(ConstFields.PATTERN_CONTAIN, StringComparison.OrdinalIgnoreCase))
            {
                return new ContainsMode();
            }
            else if (pattern.Equals(ConstFields.PATTERN_END, StringComparison.OrdinalIgnoreCase))
            {
                return new EndwithMode();
            }
            else if (pattern.Equals(ConstFields.PATTERN_EQUAL, StringComparison.OrdinalIgnoreCase))
            {
                return new EqualMode();
            }
            else if (pattern.Equals(ConstFields.PATTERN_REGULAR, StringComparison.OrdinalIgnoreCase))
            {
                return new RegularMode();
            }
            return null;
        }

        public static ICompareMode<decimal> GenerateDecimalMode(string pattern)
        {
            if (pattern.Equals(ConstFields.PATTERN_BIGGER, StringComparison.OrdinalIgnoreCase))
            {
                return new BiggerMode();
            }
            else if (pattern.Equals(ConstFields.PATTERN_SMALLER, StringComparison.OrdinalIgnoreCase))
            {
                return new SmallerMode();
            }
            else if (pattern.Equals(ConstFields.PATTERN_EQUAL, StringComparison.OrdinalIgnoreCase))
            {
                return new DeciamlEqualMode();
            }
            return null;
        }

    }
}
