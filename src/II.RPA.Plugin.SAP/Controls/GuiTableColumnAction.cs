using sapfewse;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiTableColumnAction : ISapControl
    {
        public GuiTableControl TableControl { get; set; }
        public int Column { get; set; }

        public GuiTableColumnAction(GuiComponent tableControl, int column)
        {
            TableControl = tableControl as GuiTableControl;
            if (TableControl == null)
            {
                throw new ArgumentException("The SAP component is not a GuiTableControl.", nameof(tableControl));
            }

            Column = column;
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            if (TableControl.RowCount <= 0 || TableControl.Columns.Count <= 0)
            {
                return Rectangle.Empty;
            }

            EnsureVisible();

            int firstVisibleRow = GetFirstVisibleRow();
            int visibleRows = Math.Max(1, TableControl.VisibleRowCount);
            int lastVisibleRow = Math.Min(TableControl.RowCount - 1, firstVisibleRow + visibleRows - 1);

            Rectangle result = Rectangle.Empty;
            for (int row = firstVisibleRow; row <= lastVisibleRow; row++)
            {
                try
                {
                    Rectangle cellRect = new GuiTableCellAction(TableControl, row, Column).GetRectangle(context);
                    if (cellRect.IsEmpty)
                    {
                        continue;
                    }

                    result = result.IsEmpty ? cellRect : Rectangle.Union(result, cellRect);
                }
                catch
                {
                }
            }

            return result;
        }

        public void Execute(ApplicationContext context)
        {
            if (TableControl.RowCount <= 0)
            {
                return;
            }

            new GuiTableCellAction(TableControl, GetFirstVisibleRow(), Column).Execute(context);
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                return TableControl.Columns.ElementAt(Column).title;
            }
            catch
            {
                return Column.ToString();
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            throw new NotSupportedException("SAP GuiTableControl columns do not support SetText.");
        }

        public void EnsureVisible()
        {
            if (TableControl.RowCount > 0)
            {
                new GuiTableCellAction(TableControl, GetFirstVisibleRow(), Column).EnsureVisible();
            }
        }

        public IEnumerable<int> GetAllRowIndices()
        {
            for (int i = 0; i < TableControl.RowCount; i++)
            {
                yield return i;
            }
        }

        private int GetFirstVisibleRow()
        {
            try
            {
                return Math.Max(0, TableControl.VerticalScrollbar.Position);
            }
            catch
            {
                return 0;
            }
        }
    }
}
