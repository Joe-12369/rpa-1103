using II.RPA.Plugin.Core;
using sapfewse;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Plugin.SAP.Controls
{
    public class GuiGridColumnAction : ISapControl
    {
        public GuiGridView GuiGridView { get; set; }
        public string ColumnName { get; set; }

        public GuiGridColumnAction(GuiComponent guiGridView, string columnName)
        {
            GuiGridView = guiGridView as GuiGridView;
            if (GuiGridView == null)
            {
                throw new ArgumentException("The SAP component is not a GuiGridView.", nameof(guiGridView));
            }
            ColumnName = columnName;
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            Rectangle pr = Rectangle.Empty;
            try
            {
                EnsureVisible();
                if (GuiGridView.RowCount > 0)
                {
                    int firstRow = Math.Max(0, GuiGridView.FirstVisibleRow);
                    int visibleRows = GuiGridView.VisibleRowCount > 0 ? GuiGridView.VisibleRowCount : GuiGridView.RowCount;
                    int lastRow = Math.Min(GuiGridView.RowCount - 1, firstRow + visibleRows - 1);

                    int top = GuiGridView.ScreenTop + GuiGridView.GetCellTop(firstRow, ColumnName);
                    int left = GuiGridView.ScreenLeft + GuiGridView.GetCellLeft(firstRow, ColumnName);
                    int bottom = GuiGridView.ScreenTop + GuiGridView.GetCellTop(lastRow, ColumnName)
                        + GuiGridView.GetCellHeight(lastRow, ColumnName);
                    int right = GuiGridView.ScreenLeft + GuiGridView.GetCellLeft(firstRow, ColumnName)
                        + GuiGridView.GetCellWidth(firstRow, ColumnName);

                    pr = new Rectangle(left, top, right - left, bottom - top);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GuiGridColumnAction GetRectangle error", ex);
            }
            return pr;
        }

        public void Execute(ApplicationContext context)
        {
            EnsureVisible();
            GuiGridView.SelectColumn(ColumnName);
        }

        public string GetText(ApplicationContext context)
        {
            return GuiGridView.GetDisplayedColumnTitle(ColumnName);
        }

        public void SetText(ApplicationContext context, string txt)
        {
            throw new NotSupportedException("SAP GuiGridView columns do not support SetText.");
        }

        public void EnsureVisible()
        {
            if (GuiGridView.RowCount <= 0 || IsColumnVisible())
            {
                return;
            }

            try
            {
                GuiGridView.FirstVisibleColumn = ColumnName;
                System.Threading.Thread.Sleep(100);
            }
            catch
            {
                // Some frozen or technical columns cannot become FirstVisibleColumn.
                // Keep the original SAP exception behavior for the following operation.
            }
        }

        private bool IsColumnVisible()
        {
            try
            {
                int row = Math.Max(0, GuiGridView.FirstVisibleRow);
                int left = GuiGridView.GetCellLeft(row, ColumnName);
                int width = GuiGridView.GetCellWidth(row, ColumnName);
                return width > 0 && left >= 0 && left + width <= GuiGridView.Width;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<int> GetAllRowIndices()
        {
            for (int i = 0; i < GuiGridView.RowCount; i++)
            {
                yield return i;
            }
        }
    }
}
