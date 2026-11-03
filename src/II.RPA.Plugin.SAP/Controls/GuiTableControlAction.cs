using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WinAutomation.CommonUtility;
using sapfewse;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.SAP.Controls
{
    /// <summary>
    /// GuiTableControl (经典分页表格) 操作。不持有 COM 连接，通过 SapNode.ConnectionManager 借用共享连接。
    /// </summary>
    class GuiTableControlAction : TableBase, ISapControl, ITable
    {
        private static ILogger logger = Logger.OfContext();
        private SapConnectionManager ConnectionManager => SapNode.ConnectionManager;

        public GuiTableControlAction()
        {
            // 不创建连接，借用 SapNode 的共享连接
        }

        private void RefreshSession()
        {
            ConnectionManager.RefreshSession();
        }

        private GuiTableControl RefreshTableControl(string id)
        {
            RefreshSession();
            var component = ConnectionManager.CurrentSession.FindById(id);
            GuiTableControl tableControl = component as GuiTableControl;
            return tableControl;
        }

        public void Execute(ApplicationContext context)
        {
            throw new NotImplementedException();
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            Rectangle pr = new Rectangle();
            try
            {
                GuiTableControl gb = context.component as GuiTableControl;
                if (gb != null)
                {
                    pr.X = gb.ScreenLeft;
                    pr.Y = gb.ScreenTop;
                    pr.Width = gb.Width;
                    pr.Height = gb.Height;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GuiTableControlAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            throw new NotImplementedException();
        }

        public void SetText(ApplicationContext context, string txt)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 拾取指定表格元素
        /// </summary>
        public override List<List<string>> GetTableData(string id, ParamTableModel tm)
        {
            List<List<string>> datas = new List<List<string>>();
            GuiTableControl tableControl = RefreshTableControl(id);
            var param = CheckInputForTable(tm, tableControl.RowCount - tableControl.VisibleRowCount + 1, tableControl.Columns.Count);
            if (tm.ContainHeader == 1)
            {
                datas.Add(GetCloumsHeader(tableControl, param.ColumnScope));
            }
            List<List<string>> body = new List<List<string>>();
            switch (tm.DataScope)
            {
                case 0:
                    body = FilterDataAll(tableControl, id);
                    break;
                case 1:
                    if (param.RowNo == -1)
                        body = FilterDataByRowScope(tableControl, id, param.RowScope);
                    else
                        body = FilterDataByRow(tableControl, id, param.RowNo);
                    break;
                case 2:
                    if (param.ColumnNo == -1)
                        body = FilterDataByColumnScope(tableControl, id, param.ColumnScope);
                    else
                        body = FilterDataByColumn(tableControl, id, param.ColumnNo);
                    break;
                case 3:
                    body = FilterDataByScope(tableControl, id, param.RowScope, param.ColumnScope);
                    break;
            }
            datas.AddRange(body);
            return datas;
        }

        private List<List<string>> FilterDataAll(GuiTableControl tableControl, string id)
        {
            List<List<string>> datas = new List<List<string>>();
            int columnCount = tableControl.Columns.Count;
            int visRowCount = tableControl.VisibleRowCount;//可显示的行数
            int rowCount = tableControl.RowCount;
            logger.LogInformation($"rowCount:{rowCount} visRowCount:{visRowCount}");
            tableControl.VerticalScrollbar.Position = 0;
            tableControl = RefreshTableControl(id);
            int page = rowCount / visRowCount;
            for (int i = 0; i < page; i++)
            {
                foreach (GuiTableRow row in tableControl.Rows)
                {
                    if (row == null || row.Count == 0) break;
                    List<string> columns = RowResolve(row, columnCount);
                    datas.Add(columns);
                }
                tableControl.VerticalScrollbar.Position += visRowCount;
                tableControl = RefreshTableControl(id);
            }

            return datas;
        }

        private static List<string> RowResolve(GuiTableRow row, int columnCount, int startColumn = 0)
        {
            List<string> columns = new List<string>();
            for (int k = startColumn; k < columnCount; k++)
            {
                CellResolve(row.ElementAt(k), columns);
            }

            return columns;
        }

        private static void CellResolve(GuiComponent cell, List<string> columns)
        {
            if (cell is GuiTextField a)
            {
                columns.Add(a.Text);
            }
            else if (cell is GuiLabel b)
            {
                columns.Add(b.Tooltip);
            }
            else if (cell is GuiCheckBox c)
            {
                columns.Add(c.Selected.ToString());
            }
            else if(cell  is GuiComboBox gb)
            {
                columns.Add(gb.Text);
            }
            else
            {
                columns.Add("无法获取".I18n("GuiTableControlAction_4C29ADBF"));
            }
        }

        private List<string> GetCloumsHeader(GuiTableControl tableControl, Scope columuScope)
        {
            List<string> list = new List<string>();
            for (int i = columuScope.StartNo - 1; i < columuScope.EndNo; i++)
            {
                list.Add(tableControl.Columns.ElementAt(i).title);
            }
            return list;
        }

        private List<List<string>> FilterDataByRow(GuiTableControl tableControl, string id, int rowNo)
        {
            List<List<string>> datas = new List<List<string>>();
            if (rowNo == 0)
                return datas;
            tableControl.VerticalScrollbar.Position = rowNo - 1;
            tableControl = RefreshTableControl(id);
            int columnCount = tableControl.Columns.Count;
            var row = tableControl.Rows.ElementAt(0);
            datas.Add(RowResolve(row, columnCount));
            return datas;
        }

        private List<List<string>> FilterDataByRowScope(GuiTableControl tableControl, string id, Scope rowScope)
        {
            var columnScope = new Scope(1, tableControl.Columns.Count);
            return FilterDataByScope(tableControl, id, rowScope, columnScope);
        }

        private List<List<string>> FilterDataByColumn(GuiTableControl tableControl, string id, int columnNo)
        {
            List<List<string>> datas = new List<List<string>>();
            if (columnNo == 0)
                return datas;
            int visRowCount = tableControl.VisibleRowCount;//可显示的行数
            int rowCount = tableControl.RowCount;//总行数
            logger.LogInformation($"rowCount:{rowCount} visRowCount:{visRowCount}");
            tableControl.VerticalScrollbar.Position = 0;
            tableControl = RefreshTableControl(id);
            int length = rowCount / visRowCount;
            for (int i = 0; i < length; i++)
            {
                foreach (GuiTableRow row in tableControl.Rows)
                {
                    if (row == null || row.Count == 0) break;
                    List<string> columns = new List<string>();
                    CellResolve(row.ElementAt(columnNo - 1), columns);
                    datas.Add(columns);
                }
                tableControl.VerticalScrollbar.Position += visRowCount;
                tableControl = RefreshTableControl(id);
            }
            return datas;
        }

        private List<List<string>> FilterDataByColumnScope(GuiTableControl tableControl, string id, Scope columnScope)
        {
            var rowScope = new Scope(1, tableControl.RowCount - tableControl.VisibleRowCount + 1);
            return FilterDataByScope(tableControl, id, rowScope, columnScope);
        }

        private List<List<string>> FilterDataByScope(GuiTableControl tableControl, string id, Scope rowScope, Scope columnScope)
        {
            List<List<string>> datas = new List<List<string>>();
            int visRowCount = tableControl.VisibleRowCount;//可显示的行数
            tableControl.VerticalScrollbar.Position = rowScope.StartNo - 1;//下标
            tableControl = RefreshTableControl(id);
            int completePageCount = (rowScope.EndNo - (rowScope.StartNo - 1)) / visRowCount;
            int endRowNo = (rowScope.EndNo - (rowScope.StartNo - 1)) % visRowCount;//非下标
            for (int i = 0; i < completePageCount; i++)
            {
                foreach (GuiTableRow row in tableControl.Rows)
                {
                    if (row == null || row.Count == 0) break;
                    List<string> columns = RowResolve(row, columnScope.EndNo, columnScope.StartNo - 1);
                    datas.Add(columns);
                }
                tableControl.VerticalScrollbar.Position += visRowCount;
                tableControl = RefreshTableControl(id);
            }
            if (endRowNo != 0)//可省略，留着便于理解
            {
                for (int i = 0; i < endRowNo; i++)
                {
                    GuiTableRow row = tableControl.Rows.ElementAt(i);
                    List<string> columns = RowResolve(row, columnScope.EndNo, columnScope.StartNo - 1);
                    datas.Add(columns);
                }
            }
            return datas;
        }
    }
}
