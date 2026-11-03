using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params;
using sapfewse;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.SAP.Controls
{
    public class GuiGridCellAction : ISapControl
    {
        public GuiGridView GuiGridView { get; set; }
        public bool IsHeader { get; set; }

        public int Row { get; set; }
        public string Column { get; set; }

        public int Top { get; set; }
        public int Left { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public GuiGridCellAction(GuiComponent guiGridView, int row, string column)
        {
            try
            {
                GuiGridView = guiGridView as GuiGridView;
                if (GuiGridView == null)
                {
                    throw new ArgumentException("The SAP component is not a GuiGridView.", nameof(guiGridView));
                }
                Row = row;
                Column = column;
            }
            catch (Exception)
            {
                throw;
            }

        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            RefreshLocation();
            return new Rectangle(Left, Top, Width, Height);
        }

        public void Execute(ApplicationContext context)
        {
            EnsureVisible();
            this.GuiGridView.SetCurrentCell(Row, Column);
            try
            {
                this.GuiGridView.ClickCurrentCell();
            }
            catch (ArgumentException)
            {
                // SAP raises this for normal, non-hotspot grid cells. Setting the current cell is
                // the backend equivalent of a single click for those cells.
            }
        }

        public void DoubleClick()
        {
            EnsureVisible();
            this.GuiGridView.SetCurrentCell(Row, Column);
            try
            {
                this.GuiGridView.DoubleClickCurrentCell();
            }
            catch (ArgumentException)
            {
                this.GuiGridView.DoubleClick(Row, Column);
            }
        }

        public string GetText(ApplicationContext context)
        {
            return this.GuiGridView.GetCellValue(Row, Column);
        }

        public void SetText(ApplicationContext context, string txt)
        {
            EnsureVisible();
            this.GuiGridView.SetCurrentCell(Row, Column);
            this.GuiGridView.ModifyCell(Row, Column, txt ?? string.Empty);
        }

        public void EnsureVisible()
        {
            EnsureRowVisible();
            EnsureColumnVisible();
            RefreshLocation();
        }

        private void EnsureRowVisible()
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

        private void EnsureColumnVisible()
        {
            int measureRow = GuiGridView.RowCount > 0 ? Math.Min(Math.Max(0, Row), GuiGridView.RowCount - 1) : 0;
            if (IsColumnVisible(measureRow))
            {
                return;
            }

            try
            {
                GuiGridView.FirstVisibleColumn = Column;
                System.Threading.Thread.Sleep(100);
            }
            catch
            {
                // Some frozen or technical columns cannot become FirstVisibleColumn.
                // Keep the original SAP exception behavior for the following operation.
            }
        }

        private bool IsColumnVisible(int row)
        {
            try
            {
                int left = GuiGridView.GetCellLeft(row, Column);
                int width = GuiGridView.GetCellWidth(row, Column);
                return width > 0 && left >= 0 && left + width <= GuiGridView.Width;
            }
            catch
            {
                return false;
            }
        }

        private void RefreshLocation()
        {
            Top = GuiGridView.ScreenTop + GuiGridView.GetCellTop(Row, Column);
            Left = GuiGridView.ScreenLeft + GuiGridView.GetCellLeft(Row, Column);
            Width = GuiGridView.GetCellWidth(Row, Column);
            Height = GuiGridView.GetCellHeight(Row, Column);
        }

        public static bool IsGridCell(string cellId, out int row, out string column)
        {
            var match = Regex.Match(cellId.Replace(" ", ""), @"Cell\((?<row>\d+),(?<column>.+)\)");
            if (match.Success)
            {
                row = int.Parse(match.Groups["row"].Value);
                column = match.Groups["column"].Value;
                return true;
            }
            else
            {
                row = 0;
                column = null;
                return false;
            }
        }

        /// <summary>
        /// 解析列级ext格式 "Column(MATNR)"，用于通配匹配整列
        /// </summary>
        public static bool IsGridColumn(string ext, out string columnName)
        {
            var match = Regex.Match(ext.Replace(" ", ""), @"Column\((?<column>.+)\)");
            if (match.Success)
            {
                columnName = match.Groups["column"].Value;
                return true;
            }
            else
            {
                columnName = null;
                return false;
            }
        }

        public static bool IsGridRow(string ext, out int row)
        {
            var match = Regex.Match(ext.Replace(" ", ""), @"Row\((?<row>\d+)\)");
            if (match.Success)
            {
                row = int.Parse(match.Groups["row"].Value);
                return true;
            }

            row = 0;
            return false;
        }

        internal static List<string> GetGridColumnNames(GuiGridView gridView)
        {
            object columnOrder = null;
            List<string> columns = new List<string>();
            HashSet<string> added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                columnOrder = gridView.ColumnOrder;
                AddColumnNames(columnOrder, gridView.ColumnCount, 0, columns, added);

                if (columns.Count < gridView.ColumnCount)
                {
                    AddColumnNames(columnOrder, gridView.ColumnCount, 1, columns, added);
                }
            }
            finally
            {
                if (columnOrder != null && Marshal.IsComObject(columnOrder))
                {
                    Marshal.ReleaseComObject(columnOrder);
                }
            }

            return columns;
        }

        private static void AddColumnNames(object columnOrder, int columnCount, int startIndex, List<string> columns, HashSet<string> added)
        {
            if (columnOrder is Array array)
            {
                foreach (object value in array)
                {
                    AddColumnName(value, columns, added);
                }
                return;
            }

            dynamic dynamicColumnOrder = columnOrder;
            for (int i = 0; i < columnCount; i++)
            {
                try
                {
                    AddColumnName(dynamicColumnOrder[i + startIndex], columns, added);
                }
                catch
                {
                    // Different SAP GUI versions expose ColumnOrder with slightly different COM indexers.
                }
            }
        }

        private static void AddColumnName(object value, List<string> columns, HashSet<string> added)
        {
            string column = value?.ToString();
            if (!string.IsNullOrWhiteSpace(column) && added.Add(column))
            {
                columns.Add(column);
            }
        }
    }
}
