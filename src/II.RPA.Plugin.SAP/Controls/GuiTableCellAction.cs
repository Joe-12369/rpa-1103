using sapfewse;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

namespace II.RPA.Plugin.SAP.Controls
{
    public class GuiTableCellAction : ISapControl
    {
        private static readonly Regex TableCellIdRegex = new Regex(
            @"^(?<table>.+/tbl[^/]+)/[^/]+\[(?<column>\d+),(?<row>\d+)\]$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CellTokenRegex = new Regex(
            @"\[(?<column>\d+),(?<row>\d+)\]$",
            RegexOptions.Compiled);

        public GuiTableControl TableControl { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }

        public GuiTableCellAction(GuiComponent tableControl, int row, int column)
        {
            TableControl = tableControl as GuiTableControl;
            if (TableControl == null)
            {
                throw new ArgumentException("The SAP component is not a GuiTableControl.", nameof(tableControl));
            }

            Row = row;
            Column = column;
        }

        public GuiTableCellAction(GuiTableControl tableControl, int row, int column)
        {
            TableControl = tableControl ?? throw new ArgumentNullException(nameof(tableControl));
            Row = row;
            Column = column;
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            GuiComponent cell = GetCell();
            return GetComponentRectangle(cell);
        }

        public void Execute(ApplicationContext context)
        {
            GuiComponent cell = GetCell();
            SetFocus(cell);
        }

        public void DoubleClick(ApplicationContext context)
        {
            Execute(context);
            Execute(context);
        }

        public string GetText(ApplicationContext context)
        {
            GuiComponent cell = GetCell();
            return GetComponentText(cell);
        }

        public void SetText(ApplicationContext context, string txt)
        {
            GuiComponent cell = GetCell();
            SetComponentText(cell, txt ?? string.Empty);
        }

        public void EnsureVisible()
        {
            EnsureRowVisible();
        }

        public GuiComponent GetCell()
        {
            EnsureVisible();
            ValidateCell();

            int visibleRow = Row - GetFirstVisibleRow();
            if (visibleRow < 0 || visibleRow >= TableControl.VisibleRowCount)
            {
                EnsureRowVisible();
                visibleRow = Row - GetFirstVisibleRow();
            }

            if (visibleRow < 0)
            {
                visibleRow = 0;
            }

            GuiTableRow tableRow = TableControl.Rows.ElementAt(visibleRow);
            return tableRow.ElementAt(Column);
        }

        public static bool IsTableCell(string ext, out int row, out int column)
        {
            row = 0;
            column = 0;

            if (!GuiGridCellAction.IsGridCell(ext, out int parsedRow, out string parsedColumn))
            {
                return false;
            }

            if (!int.TryParse(parsedColumn, out int parsedColumnIndex))
            {
                return false;
            }

            row = parsedRow;
            column = parsedColumnIndex;
            return true;
        }

        public static bool IsTableColumn(string ext, out int column)
        {
            column = 0;
            if (!GuiGridCellAction.IsGridColumn(ext, out string parsedColumn))
            {
                return false;
            }

            return int.TryParse(parsedColumn, out column);
        }

        public static bool TryParseTableCellId(string cellId, out string tableId, out int visibleRow, out int column)
        {
            tableId = null;
            visibleRow = 0;
            column = 0;

            if (string.IsNullOrWhiteSpace(cellId))
            {
                return false;
            }

            Match match = TableCellIdRegex.Match(cellId.Trim());
            if (!match.Success)
            {
                return false;
            }

            tableId = match.Groups["table"].Value;
            visibleRow = int.Parse(match.Groups["row"].Value);
            column = int.Parse(match.Groups["column"].Value);
            return true;
        }

        public static bool TryParseCellToken(string cellId, out int visibleRow, out int column)
        {
            visibleRow = 0;
            column = 0;

            if (string.IsNullOrWhiteSpace(cellId))
            {
                return false;
            }

            Match match = CellTokenRegex.Match(cellId.Trim());
            if (!match.Success)
            {
                return false;
            }

            visibleRow = int.Parse(match.Groups["row"].Value);
            column = int.Parse(match.Groups["column"].Value);
            return true;
        }

        public static bool TryResolveInspectCell(
            GuiSession session,
            string currentId,
            string inspectId,
            out GuiTableControl tableControl,
            out string tableId,
            out string cellExt,
            out Rectangle rectangle)
        {
            tableControl = null;
            tableId = null;
            cellExt = null;
            rectangle = Rectangle.Empty;

            if (session == null)
            {
                return false;
            }

            if (TryResolveFromFullCellId(session, inspectId, out tableControl, out tableId, out cellExt, out rectangle)
                || TryResolveFromFullCellId(session, currentId, out tableControl, out tableId, out cellExt, out rectangle))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(currentId)
                || !TryParseCellToken(inspectId, out int visibleRow, out int column))
            {
                return false;
            }

            GuiTableControl currentTable = null;
            try
            {
                currentTable = session.FindById(currentId) as GuiTableControl;
            }
            catch
            {
                currentTable = null;
            }

            if (currentTable == null)
            {
                return false;
            }

            return BuildInspectCell(currentTable, currentId, visibleRow, column, out tableControl, out tableId, out cellExt, out rectangle);
        }

        public static IEnumerable<string> GetTableColumnNames(GuiTableControl tableControl)
        {
            int count = tableControl?.Columns?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                yield return i.ToString();
            }
        }

        public static string GetComponentText(GuiComponent cell)
        {
            if (cell is GuiTextField text)
            {
                return text.Text;
            }
            if (cell is GuiCTextField cText)
            {
                return cText.Text;
            }
            if (cell is GuiLabel label)
            {
                return string.IsNullOrEmpty(label.Text) ? label.Tooltip : label.Text;
            }
            if (cell is GuiCheckBox checkBox)
            {
                return checkBox.Selected.ToString();
            }
            if (cell is GuiComboBox comboBox)
            {
                return comboBox.Text;
            }

            try
            {
                dynamic dynamicCell = cell;
                return Convert.ToString(dynamicCell.Text);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static Rectangle GetComponentRectangle(GuiComponent cell)
        {
            if (cell == null)
            {
                return Rectangle.Empty;
            }

            try
            {
                dynamic dynamicCell = cell;
                return new Rectangle(
                    Convert.ToInt32(dynamicCell.ScreenLeft),
                    Convert.ToInt32(dynamicCell.ScreenTop),
                    Convert.ToInt32(dynamicCell.Width),
                    Convert.ToInt32(dynamicCell.Height));
            }
            catch
            {
                return Rectangle.Empty;
            }
        }

        internal static void SetFocus(GuiComponent cell)
        {
            if (cell == null)
            {
                return;
            }

            if (cell is ISapLabelTarget label)
            {
                label.SetFocus();
                return;
            }
            if (cell is ISapCTextField cText)
            {
                cText.SetFocus();
                return;
            }
            if (cell is ISapTextFieldTarget text)
            {
                text.SetFocus();
                return;
            }

            try
            {
                dynamic dynamicCell = cell;
                dynamicCell.SetFocus();
            }
            catch
            {
            }
        }

        internal static void SetComponentText(GuiComponent cell, string value)
        {
            if (cell is GuiTextField text)
            {
                text.Text = value;
                return;
            }
            if (cell is GuiCTextField cText)
            {
                cText.Text = value;
                return;
            }
            if (cell is GuiComboBox comboBox)
            {
                comboBox.Text = value;
                return;
            }

            dynamic dynamicCell = cell;
            dynamicCell.Text = value;
        }

        private static bool TryResolveFromFullCellId(
            GuiSession session,
            string cellId,
            out GuiTableControl tableControl,
            out string tableId,
            out string cellExt,
            out Rectangle rectangle)
        {
            tableControl = null;
            tableId = null;
            cellExt = null;
            rectangle = Rectangle.Empty;

            if (!TryParseTableCellId(cellId, out string parsedTableId, out int visibleRow, out int column))
            {
                return false;
            }

            GuiTableControl table = null;
            try
            {
                table = session.FindById(parsedTableId) as GuiTableControl;
            }
            catch
            {
                table = null;
            }

            if (table == null)
            {
                return false;
            }

            return BuildInspectCell(table, parsedTableId, visibleRow, column, out tableControl, out tableId, out cellExt, out rectangle);
        }

        private static bool BuildInspectCell(
            GuiTableControl table,
            string id,
            int visibleRow,
            int column,
            out GuiTableControl tableControl,
            out string tableId,
            out string cellExt,
            out Rectangle rectangle)
        {
            tableControl = table;
            tableId = id;

            int row = GetAbsoluteRow(table, visibleRow);
            cellExt = $"Cell({row},{column})";

            var action = new GuiTableCellAction(table, row, column);
            rectangle = action.GetRectangle(new ApplicationContext { component = (GuiComponent)table, path = id, ext = cellExt });
            return !rectangle.IsEmpty;
        }

        private static int GetAbsoluteRow(GuiTableControl tableControl, int visibleRow)
        {
            int firstVisibleRow = 0;
            try
            {
                firstVisibleRow = Math.Max(0, tableControl.VerticalScrollbar.Position);
            }
            catch
            {
                firstVisibleRow = 0;
            }

            return firstVisibleRow + Math.Max(0, visibleRow);
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

        private void EnsureRowVisible()
        {
            if (TableControl == null || TableControl.RowCount <= 0)
            {
                return;
            }

            int visibleRows = Math.Max(1, TableControl.VisibleRowCount);
            int firstVisibleRow = GetFirstVisibleRow();
            int lastVisibleRow = firstVisibleRow + visibleRows - 1;

            if (Row >= firstVisibleRow && Row <= lastVisibleRow)
            {
                return;
            }

            int maxFirstVisibleRow = Math.Max(0, TableControl.RowCount - visibleRows);
            int targetFirstVisibleRow = Math.Min(Math.Max(0, Row), maxFirstVisibleRow);
            try
            {
                TableControl.VerticalScrollbar.Position = targetFirstVisibleRow;
                System.Threading.Thread.Sleep(100);
            }
            catch
            {
            }
        }

        private void ValidateCell()
        {
            if (Row < 0 || Row >= TableControl.RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(Row));
            }
            if (Column < 0 || Column >= TableControl.Columns.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(Column));
            }
        }
    }
}
