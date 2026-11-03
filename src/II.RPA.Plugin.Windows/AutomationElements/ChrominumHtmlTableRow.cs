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
    internal class ChrominumHtmlTableRow : AutomationElement
    {
        /// <summary>
        /// Creates a <see cref="Label"/> element.
        /// </summary>
        public ChrominumHtmlTableRow(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {

        }
        public IEnumerable<string> RowData
        {
            get
            {
                
                var cellElements = this.FindAllChildren();
                foreach (var cellElement in cellElements)
                {
                    if (cellElement.IsPageTableCell())
                    {
                        yield return cellElement.AsChrominumHtmlTableCell().Text;
                    }
                    else if (cellElement.IsPageTableColumnHeader())
                    {
                        yield return cellElement.AsChrominumHtmlTableColumnHeader().Text;
                    }
                }
            }
        }
    }
}
