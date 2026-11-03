using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.JavaBridgeHost.CompareRule
{
    public static class RuleFactory
    {
        public static bool IsDecimalRule(string pattern)
        {
            if (ConstFields.PATTERN_BIGGER == pattern || ConstFields.PATTERN_SMALLER == pattern)
            {
                return true;
            }
            return false;
        }

        public static ICompareRule<string> GenerateStringRule(string pattern) 
        {
            if (pattern.Equals(ConstFields.PATTERN_BEGIN, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleStartwith();
            }
            else if (pattern.Equals(ConstFields.PATTERN_CONTAIN, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleContains();
            }
            else if (pattern.Equals(ConstFields.PATTERN_END, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleEndwith();
            }
            else if (pattern.Equals(ConstFields.PATTERN_EQUAL, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleEqual();
            }
            else if (pattern.Equals(ConstFields.PATTERN_REGULAR, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleRuleRegular();
            }
            return null;
        }

        public static ICompareRule<decimal> GenerateDecimalRule(string pattern) 
        {
            if (pattern.Equals(ConstFields.PATTERN_BIGGER, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleBiggerRule();
            }
            else if (pattern.Equals(ConstFields.PATTERN_SMALLER, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleSmallerRule();
            }
            else if (pattern.Equals(ConstFields.PATTERN_EQUAL, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleDeciamlEqualRule();
            }
            return null;
        }

    }
}
