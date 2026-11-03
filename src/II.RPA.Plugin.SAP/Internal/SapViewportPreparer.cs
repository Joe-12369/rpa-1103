using sapfewse;
using System;
using System.Threading;

namespace II.RPA.Plugin.SAP.Internal
{
    /// <summary>
    /// 在 SAP GUI 操作前将目标控件滚动到可见区域。
    /// 利用 SAP 特性：对控件调用 SetFocus() 会自动将其滚动到可视区。
    /// </summary>
    internal static class SapViewportPreparer
    {
        /// <summary>
        /// 确保 GuiVComponent 在可视区域内（通过 SetFocus 触发自动滚动）。
        /// </summary>
        public static bool TryEnsureVisible(GuiComponent component)
        {
            var vComponent = component as GuiVComponent;
            if (vComponent == null)
                return false;

            try
            {
                vComponent.SetFocus();
                Thread.Sleep(100);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 确保 GridView 的指定单元格在可视区域内。
        /// </summary>
        public static bool TryEnsureGridCellVisible(GuiGridView gridView, int row, string columnForApi)
        {
            bool adjusted = false;

            try
            {
                if (gridView == null || string.IsNullOrEmpty(columnForApi))
                    return false;

                int measureRow = 0;
                if (gridView.RowCount > 0)
                {
                    int visibleRows = Math.Max(1, gridView.VisibleRowCount);
                    int firstVisibleRow = Math.Max(0, gridView.FirstVisibleRow);
                    int lastVisibleRow = firstVisibleRow + visibleRows - 1;
                    measureRow = Math.Min(Math.Max(0, row), gridView.RowCount - 1);

                    if (row < firstVisibleRow || row > lastVisibleRow)
                    {
                        int maxFirstVisibleRow = Math.Max(0, gridView.RowCount - visibleRows);
                        int targetFirstVisibleRow = Math.Min(Math.Max(0, row), maxFirstVisibleRow);
                        if (targetFirstVisibleRow != firstVisibleRow)
                        {
                            gridView.FirstVisibleRow = targetFirstVisibleRow;
                            adjusted = true;
                        }
                    }
                }

                if (!IsGridColumnVisible(gridView, measureRow, columnForApi))
                {
                    try
                    {
                        gridView.FirstVisibleColumn = columnForApi;
                        adjusted = true;
                    }
                    catch
                    {
                        // Some frozen or technical columns cannot become FirstVisibleColumn.
                        // Keep the original SAP exception behavior for the following operation.
                    }
                }
            }
            catch
            {
                // ignore
            }

            if (adjusted)
                Thread.Sleep(100);

            return adjusted;
        }

        private static bool IsGridColumnVisible(GuiGridView gridView, int row, string columnForApi)
        {
            try
            {
                int left = gridView.GetCellLeft(row, columnForApi);
                int width = gridView.GetCellWidth(row, columnForApi);
                return width > 0 && left >= 0 && left + width <= gridView.Width;
            }
            catch
            {
                return false;
            }
        }
    }
}
