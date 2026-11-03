using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Windows.CompareRule
{
    public static class RuleFactory
    {
        public static bool IsDecimalRule(string pattern)
        {
            //这两应该不用了，先留着吧
            if (ConstFields.PATTERN_BIGGER == pattern || ConstFields.PATTERN_SMALLER == pattern)
            {
                return true;
            }
            if (ConstFields.PATTERN_LESSTHAN == pattern || ConstFields.PATTERN_LESSTHANOREQUAL == pattern
                || ConstFields.PATTERN_MORETHAN == pattern || ConstFields.PATTERN_MORETHANOREQUAL == pattern)
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
            else if (pattern.Equals(ConstFields.PATTERN_NOTCONTAIN, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleNotContains();
            }
            else if (pattern.Equals(ConstFields.PATTERN_NOTBEGIN, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleNotStartwith();
            }
            else if (pattern.Equals(ConstFields.PATTERN_NOTEND, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleNotEndwith();
            }
            else if (pattern.Equals(ConstFields.PATTERN_EXCLUDE, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleNotEqual();
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
            else if (pattern.Equals(ConstFields.PATTERN_MORETHAN, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleMoreThanRule();
            }
            else if (pattern.Equals(ConstFields.PATTERN_MORETHANOREQUAL, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleMoreThanOrEqualRule();
            }
            else if (pattern.Equals(ConstFields.PATTERN_LESSTHAN, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleLessThanRule();
            }
            else if (pattern.Equals(ConstFields.PATTERN_LESSTHANOREQUAL, StringComparison.OrdinalIgnoreCase))
            {
                return new RuleLessThanOrEqualRule();
            }
            return null;
        }

    }
}
