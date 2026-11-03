using sapfewse;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiTableRowAction : ISapControl
    {
        public GuiTableControl TableControl { get; set; }
        public int Row { get; set; }

        public GuiTableRowAction(GuiComponent tableControl, int row)
        {
            TableControl = tableControl as GuiTableControl;
            if (TableControl == null)
            {
                throw new ArgumentException("The SAP component is not a GuiTableControl.", nameof(tableControl));
            }

            Row = row;
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            if (TableControl.RowCount <= 0 || TableControl.Columns.Count <= 0)
            {
                return Rectangle.Empty;
            }

            EnsureVisible();

            Rectangle result = Rectangle.Empty;
            foreach (string columnName in GuiTableCellAction.GetTableColumnNames(TableControl))
            {
                if (!int.TryParse(columnName, out int column))
                {
                    continue;
                }

                try
                {
                    Rectangle cellRect = new GuiTableCellAction(TableControl, Row, column).GetRectangle(context);
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
            if (TableControl.Columns.Count <= 0)
            {
                return;
            }

            new GuiTableCellAction(TableControl, Row, 0).Execute(context);
        }

        public string GetText(ApplicationContext context)
        {
            EnsureVisible();
            List<string> values = new List<string>();
            foreach (string columnName in GuiTableCellAction.GetTableColumnNames(TableControl))
            {
                if (!int.TryParse(columnName, out int column))
                {
                    continue;
                }

                try
                {
                    values.Add(new GuiTableCellAction(TableControl, Row, column).GetText(context));
                }
                catch
                {
                    values.Add(string.Empty);
                }
            }

            return string.Join("\t", values);
        }

        public void SetText(ApplicationContext context, string txt)
        {
            throw new NotSupportedException("SAP GuiTableControl rows do not support SetText.");
        }

        public void EnsureVisible()
        {
            if (TableControl.Columns.Count > 0)
            {
                new GuiTableCellAction(TableControl, Row, 0).EnsureVisible();
            }
        }
    }
}
