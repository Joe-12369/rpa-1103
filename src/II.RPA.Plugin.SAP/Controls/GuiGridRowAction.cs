using II.RPA.Plugin.Core;
using sapfewse;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace II.RPA.Plugin.SAP.Controls
{
    public class GuiGridRowAction : ISapControl
    {
        public GuiGridView GuiGridView { get; set; }
        public int Row { get; set; }

        public GuiGridRowAction(GuiComponent guiGridView, int row)
        {
            GuiGridView = guiGridView as GuiGridView;
            if (GuiGridView == null)
            {
                throw new ArgumentException("The SAP component is not a GuiGridView.", nameof(guiGridView));
            }

            Row = row;
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            EnsureVisible();

            List<string> columns = GuiGridCellAction.GetGridColumnNames(GuiGridView);
            if (columns.Count == 0)
            {
                return Rectangle.Empty;
            }

            int top = 0;
            int height = 0;
            int minLeft = int.MaxValue;
            int maxRight = int.MinValue;
            bool hasCell = false;

            foreach (string column in columns)
            {
                try
                {
                    int left = GuiGridView.GetCellLeft(Row, column);
                    int width = GuiGridView.GetCellWidth(Row, column);
                    int cellTop = GuiGridView.GetCellTop(Row, column);
                    int cellHeight = GuiGridView.GetCellHeight(Row, column);

                    if (width <= 0 || cellHeight <= 0)
                    {
                        continue;
                    }

                    int right = left + width;
                    if (right <= 0 || left >= GuiGridView.Width)
                    {
                        continue;
                    }

                    if (!hasCell)
                    {
                        top = cellTop;
                        height = cellHeight;
                        hasCell = true;
                    }

                    minLeft = Math.Min(minLeft, Math.Max(0, left));
                    maxRight = Math.Max(maxRight, Math.Min(GuiGridView.Width, right));
                }
                catch
                {
                    // Ignore technical or currently unavailable columns when calculating the visible row rectangle.
                }
            }

            if (!hasCell || maxRight <= minLeft)
            {
                string firstColumn = columns.First();
                int rowTop = GuiGridView.GetCellTop(Row, firstColumn);
                int rowHeight = GuiGridView.GetCellHeight(Row, firstColumn);
                return new Rectangle(GuiGridView.ScreenLeft, GuiGridView.ScreenTop + rowTop, GuiGridView.Width, rowHeight);
            }

            return new Rectangle(
                GuiGridView.ScreenLeft + minLeft,
                GuiGridView.ScreenTop + top,
                maxRight - minLeft,
                height);
        }

        public void Execute(ApplicationContext context)
        {
            EnsureVisible();
            try
            {
                GuiGridView.SelectedRows = Row.ToString();
            }
            catch
            {
                string firstColumn = GuiGridCellAction.GetGridColumnNames(GuiGridView).FirstOrDefault();
                if (!string.IsNullOrEmpty(firstColumn))
                {
                    GuiGridView.SetCurrentCell(Row, firstColumn);
                    GuiGridView.ClickCurrentCell();
                }
            }
        }

        public string GetText(ApplicationContext context)
        {
            List<string> values = new List<string>();
            foreach (string column in GuiGridCellAction.GetGridColumnNames(GuiGridView))
            {
                try
                {
                    values.Add(GuiGridView.GetCellValue(Row, column));
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
            throw new NotSupportedException("SAP GuiGridView rows do not support SetText.");
        }

        public void EnsureVisible()
        {
            if (GuiGridView.RowCount <= 0)
            {
                return;
            }

            int visibleRows = Math.Max(1, GuiGridView.VisibleRowCount);
            int firstVisibleRow = Math.Max(0, GuiGridView.FirstVisibleRow);
            int lastVisibleRow = firstVisibleRow + visibleRows - 1;

            if (Row >= firstVisibleRow && Row <= lastVisibleRow)
            {
                return;
            }

            int maxFirstVisibleRow = Math.Max(0, GuiGridView.RowCount - visibleRows);
            int targetFirstVisibleRow = Math.Min(Math.Max(0, Row), maxFirstVisibleRow);
            GuiGridView.FirstVisibleRow = targetFirstVisibleRow;
            System.Threading.Thread.Sleep(100);
        }
    }
}
