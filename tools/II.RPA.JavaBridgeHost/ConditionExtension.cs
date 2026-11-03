using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.JavaBridgeHost.CompareRule;

namespace II.RPA.JavaBridgeHost
{
    public static class ConditionExtension
    {
        internal static bool ConiditionMatch(this JavaSelectorItemProperty source, string propValue)
        {
            if (source.Value.Contains(ConstFields.COMPARE_SYMBOL_STAR) && 
                source.SelectorVersion == ConstFields.SELECTOR_OLD_FORMAT_VERSION)
            {
                return PatternMatcher.FitsMask(propValue, source.Value);

            }
            else
            {
                if (RuleFactory.IsDecimalRule(source.Pattern))
                {
                    var rule = RuleFactory.GenerateDecimalRule(source.Pattern);
                    decimal d1 = 0;
                    decimal d2 = 0;
                    if (rule != null && decimal.TryParse(source.Value, out d1) && decimal.TryParse(propValue, out d2))
                    {
                        return rule.Compare(d1, d2);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    var rule = RuleFactory.GenerateStringRule(source.Pattern);
                    if (rule != null)
                    {
                        return rule.Compare(propValue, source.Value);
                    }
                    return false;
                }
            }
        }

    }
}
