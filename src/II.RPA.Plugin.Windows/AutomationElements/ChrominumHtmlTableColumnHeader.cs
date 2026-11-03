using FlaUI.Core.AutomationElements;
using FlaUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FlaUI.Core.FrameworkAutomationElementBase;

namespace II.RPA.Plugin.Windows.AutomationElements
{
    internal class ChrominumHtmlTableColumnHeader : AutomationElement
    {
        /// <summary>
        /// Creates a <see cref="Label"/> element.
        /// </summary>
        public ChrominumHtmlTableColumnHeader(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {

        }
        /// <summary>
        /// Gets the text of the element.
        /// </summary>
        public string Text
        {
            get
            {
                if (Patterns.LegacyIAccessible.IsSupported
                   && Patterns.LegacyIAccessible.Pattern.Name.TryGetValue(out var eleName)
                   && !string.IsNullOrEmpty(eleName))
                {
                    return eleName;
                }
                return Properties.Name.Value;
            }
        }
    }
}
