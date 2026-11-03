using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WinAutomation.CommonUtility;
using sapfewse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.SAP.Controls
{
    /// <summary>
    /// GuiGridView (ALV) 表格操作。不持有 COM 连接，通过 SapNode.ConnectionManager 借用共享连接。
    /// </summary>
    public class GuiGridViewAction : TableBase, ITable
    {
        private SapConnectionManager ConnectionManager => SapNode.ConnectionManager;

        public GuiGridViewAction()
        {
            // 不创建连接，借用 SapNode 的共享连接
        }

        private void RefreshSession()
        {
            ConnectionManager.RefreshSession();
        }

        private T RefreshControl<T>(string id)
        {
            RefreshSession();
            T component = (T)ConnectionManager.CurrentSession.FindById(id);
            return component;
        }

        //SelectAll 引发异常
        [Obsolete]
        private List<string> GetColumnsHeaderKey(GuiGridView gridView, Scope columnScope)
        {
            gridView.SelectAll();
            List<string> columns = new List<string>();
            foreach (var colName in gridView.SelectedColumns)
            {
                columns.Add(colName);                 //列名
            }

            List<string> headKeys = new List<string>();
            for (int i = columnScope.StartNo - 1; i < columnScope.EndNo; i++)
            {
                headKeys.Add(columns[i]);
            }
            gridView.ClearSelection();
            return headKeys;
        }
        private List<string> GetColumnsHeaderKey(GuiGridView gridView)
        {
            List<string> headKeys = new List<string>();
            var orders = gridView.ColumnOrder;
            for (int i = 0; i < gridView.ColumnCount; i++)
            {
                headKeys.Add((string)orders[i]);
            }
            if (orders != null && Marshal.IsComObject(orders))
            {
                Marshal.ReleaseComObject(orders);
            }
            return headKeys;
        }

        private List<string> GetColumnsHeader(GuiGridView view, List<string> headKeys)
        {
            List<string> headS = new List<string>();
            foreach (var colName in headKeys)
            {
                headS.Add(view.GetDisplayedColumnTitle(colName));
            }
            return headS;
        }

        public override List<List<string>> GetTableData(string id, ParamTableModel tm)
        {
            GuiGridView gridView = RefreshControl<GuiGridView>(id);
            if (gridView != null)
            {
                var param = CheckInputForTable(tm, gridView.RowCount, gridView.ColumnCount);
                List<List<string>> datas = new List<List<string>>();
                //List<string> columns = GetColumnsHeaderKey(gridView, param.ColumnScope);

                // change by wenyi ,fixed selectAll method exception
                List<string> columns = GetColumnsHeaderKey(gridView);
                if (tm.ContainHeader == 1)
                {
                    datas.Add(GetColumnsHeader(gridView, columns));
                }
                if (param.RowScope.StartNo != 0)
                {
                    for (int i = param.RowScope.StartNo - 1; i < param.RowScope.EndNo; i++)
                    {
                        List<string> subDatas = new List<string>();
                        foreach (var columnName in columns)
                        {
                            subDatas.Add(gridView.GetCellValue(i, columnName));
                        }
                        datas.Add(subDatas);
                    }
                }
                if (tm.ContainHeader != 1)
                {
                    var rowCount = datas.FirstOrDefault().Count;
                    List<string> rowFirst = new List<string>();
                    for (int i = 0; i < rowCount; i++)
                    {
                        rowFirst.Add("");
                    }
                    datas.Insert(0, rowFirst);
                }
                return datas;
            }
            return null;
        }
    }
}
