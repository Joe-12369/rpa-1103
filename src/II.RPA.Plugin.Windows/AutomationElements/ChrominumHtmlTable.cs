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
    internal class ChrominumHtmlTable : AutomationElement
    {
        /// <summary>
        /// Creates a <see cref="Label"/> element.
        /// </summary>
        public ChrominumHtmlTable(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {

        }
        private AutomationElement firstRowElement = null;
        public AutomationElement FirstRowElement
        {
            get
            {
                if (firstRowElement == null)
                {
                    var firstElement = this.FindFirstChild();
                    if (firstElement != null && firstElement.IsPageTableRow())
                    {
                        firstRowElement = firstElement;
                    }
                }
                return firstRowElement;
            }
        }
        public IEnumerable<IEnumerable<string>> TableData
        {
            get
            {
                List<IList<string>> table = new List<IList<string>>();
                var rowElements = this.FindAllChildren();
                if (!this.HasHeader)
                {
                    yield return HeaderRowData;
                }
                foreach (var rowElement in rowElements)
                {
                    if (!rowElement.IsPageTableRow())
                        continue;
                    yield return rowElement.AsChrominumHtmlTableRow().RowData;
                }
            }
        }
        public bool HasHeader
        {
            get
            {
                var cellElement = this.FirstRowElement?.FindFirstChild();
                if (cellElement == null || !cellElement.IsPageTableColumnHeader())
                {
                    return false;
                }
                return true;
            }
        }
        public IEnumerable<string> HeaderRowData
        {
            get
            {
                var cellElements = this.FirstRowElement?.FindAllChildren();
                if (cellElements.Any(cell => cell.IsPageTableColumnHeader()))
                {
                    return firstRowElement.AsChrominumHtmlTableRow().RowData;
                }
                else
                {
                    return cellElements.Where(cell => cell.IsPageTableCell())
                                       .Select(cell => string.Empty);
                }
            }
        }


    }
}
