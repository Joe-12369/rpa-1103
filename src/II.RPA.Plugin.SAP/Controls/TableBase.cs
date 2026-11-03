using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.SAP.Controls
{
    public abstract class TableBase : ITable
    {
        private static ILogger logger = Logger.OfContext();

        public abstract List<List<string>> GetTableData(string id, ParamTableModel tm);

        public virtual ScopeRowColumn CheckInputForTable(ParamTableModel tm, int rowCount, int columnCount)
        {
            logger.LogDebug($"CheckInputForTable:input{tm} rowCount:{rowCount} columnCount:{columnCount}");
            string pattern = @"^\d+(-\d+)?$";
            Regex reg = new Regex(pattern);
            switch (tm.DataScope)
            {
                case 0:
                    return new ScopeRowColumn(0, 0, new Scope(1, rowCount), new Scope(1, columnCount));
                case 1:
                    if (!reg.IsMatch(tm.RowNo))
                    {
                        throw new SAPException(eErrorMessageType.Error_TableRowNO);
                    }
                    string[] strs = tm.RowNo.Split('-');
                    if (strs.Length == 1)
                    {
                        int.TryParse(tm.RowNo, out int rowNo);
                        if (rowNo > rowCount)
                        {
                            throw new SAPException(eErrorMessageType.Error_TableRowExceed);
                        }
                        return new ScopeRowColumn(rowNo, 0, new Scope(rowNo, rowNo), new Scope(1, columnCount));
                    }
                    else
                    {
                        Scope row = GetScope(tm.RowNo);
                        if (row.StartNo > row.EndNo)
                            throw new SAPException(eErrorMessageType.Error_TableStartRowExceed);
                        if (row.EndNo > rowCount)
                            throw new SAPException(eErrorMessageType.Error_TableRowExceed);
                        if (row.StartNo == 0) row.StartNo = 1;
                        return new ScopeRowColumn(-1, 0, row, new Scope(1, columnCount));
                    }
                case 2:
                    if (!reg.IsMatch(tm.ColumnNo))
                    {
                        throw new SAPException(eErrorMessageType.Error_TableColumnNo);
                    }
                    strs = tm.ColumnNo.Split('-');
                    if (strs.Length == 1)
                    {
                        int.TryParse(tm.ColumnNo, out int columnNo);
                        if (columnNo > columnCount)
                        {
                            throw new SAPException(eErrorMessageType.Error_TableColumnExceed);
                        }
                        if (columnNo <= 0)
                        {
                            throw new SAPException(eErrorMessageType.Error_TableColumnNonzero);
                        }
                        return new ScopeRowColumn(0, columnNo, new Scope(1, rowCount), new Scope(columnNo, columnNo));
                    }
                    else
                    {
                        Scope column = GetScope(tm.ColumnNo);
                        if (column.StartNo > column.EndNo)
                            throw new SAPException(eErrorMessageType.Error_TableStartColumnExceed);
                        if (column.EndNo > columnCount)
                            throw new SAPException(eErrorMessageType.Error_TableColumnExceed);
                        if (column.StartNo == 0) column.StartNo = 1;
                        return new ScopeRowColumn(0, -1, new Scope(1, rowCount), column);
                    }
                case 3:
                    var rows = GetScope(tm.RowNo);
                    if (rows.StartNo > rows.EndNo)
                    {
                        throw new SAPException(eErrorMessageType.Error_TableStartRowExceed);
                    }
                    var columns = GetScope(tm.ColumnNo);
                    if (columns.StartNo > columns.EndNo)
                    {
                        throw new SAPException(eErrorMessageType.Error_TableStartColumnExceed);
                    }
                    //if (rows.Item1 <= 0)
                    //{
                    //    throw new Exception("行号必须为大于零的整数");
                    //}
                    if (rows.EndNo > rowCount)
                    {
                        throw new SAPException(eErrorMessageType.Error_TableRowExceed);
                    }
                    if (columns.StartNo <= 0)
                    {
                        throw new SAPException(eErrorMessageType.Error_TableColumnNonzero);
                    }
                    if (columns.EndNo > columnCount)
                    {
                        throw new SAPException(eErrorMessageType.Error_TableColumnExceed);
                    }
                    return new ScopeRowColumn(0, 0, rows, columns);
                default:
                    return new ScopeRowColumn(0, 0, new Scope(1, rowCount), new Scope(1, columnCount));
            }
        }

        public Scope GetScope(string scopeDesc)
        {
            logger.LogInformation($"1scopeDesc:{scopeDesc}");
            string[] scopes = scopeDesc.Split('-');
            if (scopes.Length != 2)
            {
                throw new SAPException(eErrorMessageType.Error_Format_Error);
            }
            bool result = int.TryParse(scopes[0], out int start);
            logger.LogInformation($"2scopeDesc:{scopeDesc}");
            if (result == false)
            {
                throw new SAPException(eErrorMessageType.Error_StartNO_Error);
            }
            result = int.TryParse(scopes[1], out int end);
            logger.LogInformation($"3scopeDesc:{scopeDesc}");
            if (result == false)
            {
                throw new SAPException(eErrorMessageType.Error_EndNO_Error);
            }
            return new Scope(start, end);
        }
    }
}
