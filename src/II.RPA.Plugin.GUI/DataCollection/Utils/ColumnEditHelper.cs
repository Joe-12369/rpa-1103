using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace II.RPA.Plugin.GUI.DataCollection.Utils
{
    #region 列名与索引转换
    public class ColumnIDHelper
    {
        /// <summary>
        /// 将 Excle 列索引转换成列名称
        /// </summary>
        /// <param name="colIndex">列索引</param>
        /// <returns></returns>
        public static string NumberToColumnID(int colIndex)
        {
            colIndex++;
            string strRtn = "";
            if (colIndex <= 0)
                return "";
            int numMod = 0;
            int numTemp = colIndex;
            char ch = 'A';
            do
            {
                numMod = numTemp % 26;
                numTemp = numTemp / 26;
                if (numMod == 0)
                {
                    ch = 'Z';
                    numTemp--;
                }
                else
                    ch = (char)('A' + numMod - 1);
                strRtn = ch + strRtn;
            }
            while (numTemp > 0);
            return strRtn;
        }

        /// <summary>
        /// 列名转数字
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int ColumnIDToNumber(string s)
        {
            int sum = 0;
            for (int i = 0; i < s.Length; ++i)
            {
                sum = sum * 26 + (s[i] - 'A');
            }
            sum--;
            return sum;
        }
    }
    #endregion

    public class ColumnEditHelper
    {
        /// <summary>
        /// 根据列名获取索引
        /// </summary>
        /// <param name="_ColName"></param>
        /// <param name="_DataTable"></param>
        /// <returns></returns>
        public static int GetColumnIndexbyName(string _ColName, DataTable _DataTable)
        {
            int index = -1;
            for (int i = 0; i < _DataTable.Columns.Count; i++)
            {
                if (_DataTable.Columns[i].ColumnName == _ColName)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

    }

    #region DataTable 操作
    public class DataTableHelper
    {

        /// <summary>
        /// 根据列名获取索引
        /// </summary>
        /// <param name="_ColName"></param>
        /// <param name="_DataTable"></param>
        /// <returns></returns>
        public static int GetColumnIndexbyName(DataTable _DataTable, string _ColName)
        {
            int index = -1;
            for (int i = 0; i < _DataTable.Columns.Count; i++)
            {
                if (_DataTable.Columns[i].ColumnName == _ColName)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        /// <summary>
        /// 清空列 
        /// </summary>
        #region 清空列
        public static void EmptyColumn(DataTable _DataTable, int _CurColumnIndex, int MaxRowCountExceptCurrentCol)
        {
            for (int i = 0; i < _DataTable.Rows.Count; i++)
            {
                _DataTable.Rows[i][_CurColumnIndex] = "";
            }

            if (_DataTable.Rows.Count > MaxRowCountExceptCurrentCol)
            {
                for (int i= _DataTable.Rows.Count-1; i >= MaxRowCountExceptCurrentCol; i--)
                {
                    _DataTable.Rows.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 清空列 old
        /// </summary>
        /// <param name="_DataTable"></param>
        /// <param name="_CurColumnIndex"></param>
        public static void EmptyColumn(DataTable _DataTable, int _CurColumnIndex)
        {
            for (int i = 0; i < _DataTable.Rows.Count; i++)
            {
                _DataTable.Rows[i][_CurColumnIndex] = "";
            }
        }
        #endregion


        /// <summary>
        /// 重新赋值列
        /// </summary>
        #region  重写
        public static void ReWriteColumn(DataTable _DataTable, int _CurColumnIndex, List<string> _ContentList)
        {
            if (_ContentList.Count <= _DataTable.Rows.Count)
            {
                for (int i = 0; i < _DataTable.Rows.Count; i++)
                {
                    if (i < _ContentList.Count)
                    {
                        _DataTable.Rows[i][_CurColumnIndex] = _ContentList[i];
                    }
                    else
                    {
                        _DataTable.Rows[i][_CurColumnIndex] = "";
                    }
                }
            }
            else
            {
                int iRowIncrease = _ContentList.Count - _DataTable.Rows.Count;
                for (int i=0;i< iRowIncrease;i++)
                {
                    _DataTable.Rows.Add();
                }

                for (int i = 0; i < _DataTable.Rows.Count; i++)
                {
                    _DataTable.Rows[i][_CurColumnIndex] = _ContentList[i];
                }
            }
        }

        /// <summary>
        /// 重新赋值列 new
        /// </summary>
        /// <param name="_DataTable"></param>
        /// <param name="_CurColumnIndex"></param>
        /// <param name="_ContentList"></param>
        public static void ReWriteColumn(DataTable _DataTable, int _CurColumnIndex, List<string> _ContentList, int MaxRowCountExceptCurrentCol, bool _IsCoverAll = true)
        {
            if (_ContentList.Count <= _DataTable.Rows.Count)//有减少当前行数的可能
            {
                int _MaxValue = Math.Max(MaxRowCountExceptCurrentCol, _ContentList.Count);
                if (_DataTable.Rows.Count > _MaxValue)
                {
                    for (int i = _DataTable.Rows.Count - 1; i >= _MaxValue; i--)
                    {
                        _DataTable.Rows.RemoveAt(i);
                    }
                }

                if (_IsCoverAll)
                {
                    for (int i = 0; i < _DataTable.Rows.Count; i++)
                    {
                        if (i < _ContentList.Count)
                        {
                            _DataTable.Rows[i][_CurColumnIndex] = _ContentList[i];
                        }
                        else
                        {
                            _DataTable.Rows[i][_CurColumnIndex] = "";
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _ContentList.Count; i++)
                    {
                        _DataTable.Rows[i][_CurColumnIndex] = _ContentList[i];
                    }
                }
            }
            else
            {
                int iRowIncrease = _ContentList.Count - _DataTable.Rows.Count;
                for (int i = 0; i < iRowIncrease; i++)
                {
                    _DataTable.Rows.Add();
                }

                for (int i = 0; i < _DataTable.Rows.Count; i++)
                {
                    _DataTable.Rows[i][_CurColumnIndex] = _ContentList[i];
                }
            }
        }
        #endregion



        /// <summary>
        /// DataTable 新增列
        /// </summary>
        /// <param name="ItemContents"></param>
        /// <param name="_DataTable"></param>
        /// <param name="strColumnName"></param>
        /// <param name=""></param>
        #region 新增列
        public static void AddNewColumn(DataTable _DataTable, List<string> ItemContents, string strColumnName = "")
        {
            string _ColumnName = ColumnIDHelper.NumberToColumnID(_DataTable.Columns.Count);

            if (!string.IsNullOrEmpty(strColumnName))
            {
                _ColumnName = $"{_ColumnName} {strColumnName}"; //strColumnName;//
            }

            DataColumn DC = new DataColumn($"{_ColumnName}");
            if (_DataTable.Columns.Count == 0)
            {
                _DataTable.Columns.Add(DC);
                for (int i = 0; i < ItemContents.Count; i++)
                {
                    _DataTable.Rows.Add();
                    _DataTable.Rows[i][0] = ItemContents[i];
                }
            }
            else
            {
                _DataTable.Columns.Add(DC);
                if (_DataTable.Rows.Count >= ItemContents.Count)
                {
                    for (int i = 0; i < ItemContents.Count; i++)
                    {
                        _DataTable.Rows[i][_DataTable.Columns.Count - 1] = ItemContents[i];
                    }
                }
                else
                {
                    for (int i = 0; i < _DataTable.Rows.Count; i++)
                    {
                        _DataTable.Rows[i][_DataTable.Columns.Count - 1] = ItemContents[i];
                    }
                    int count = _DataTable.Rows.Count;
                    for (int i = count; i < ItemContents.Count; i++)
                    {
                        _DataTable.Rows.Add();
                        _DataTable.Rows[i][_DataTable.Columns.Count - 1] = ItemContents[i];
                    }
                }
            }
        }
        #endregion


        /// <summary>
        /// 重写Table
        /// </summary>
        /// <param name="_DataTable"></param>
        /// <param name="_ContentList"></param>
        #region 重写
        public static void ReWriteTable(ref DataTable _DataTable, List<List<string>> _ContentList, List<ColNameStruct> ColNameList)
        {
            _DataTable = new DataTable();
            if (_ContentList == null || _ContentList.Count == 0 || _ContentList[0].Count == 0)//空表格
            {
                DataColumn dataColumn = new DataColumn($"A");
                _DataTable.Columns.Add(dataColumn);
                DataRow dr = _DataTable.NewRow(); //字段对象
                _DataTable.Rows.Add(dr); //添加给表
                return;
            }

            int iColumnCount = _ContentList[0].Count;

            if (ColNameList.Count == 0)//列名集合为空
            {
                if (VerifyEmptyRow(_ContentList[0]) == true)//第一行是空
                {
                    for (int i = 0; i < iColumnCount; i++)
                    {
                        //DataColumn dataColumn = new DataColumn($"表格数据{ColumnIDHelper.NumberToColumnID(i)}");
                        DataColumn dataColumn = new DataColumn($"{ColumnIDHelper.NumberToColumnID(i)}");
                        _DataTable.Columns.Add(dataColumn);
                    }
                }
                else
                {
                    for (int i = 0; i < iColumnCount; i++)
                    {
                        DataColumn dataColumn = string.IsNullOrEmpty(_ContentList[0][i]) ? new DataColumn(ColumnIDHelper.NumberToColumnID(i)) : new DataColumn((ColumnIDHelper.NumberToColumnID(i) + " " + _ContentList[0][i]).Trim());
                        _DataTable.Columns.Add(dataColumn);
                    }
                }
            }
            else //添加了列名定义
            {
                if (VerifyEmptyRow(_ContentList[0]) == true)//第一行是空
                {
                    for (int i = 0; i < iColumnCount; i++)
                    {
                        DataColumn dataColumn = null;
                        if (ColNameList.Count > i && ColNameList[i].HasSet == 1 && !string.IsNullOrEmpty(ColNameList[i].ColName))
                        {
                            dataColumn = new DataColumn((ColumnIDHelper.NumberToColumnID(i) + " " + ColNameList[i].ColName).Trim());
                        }
                        else
                        {
                            dataColumn = new DataColumn($"{ColumnIDHelper.NumberToColumnID(i)}");
                        }
                        
                        _DataTable.Columns.Add(dataColumn);
                    }
                }
                else
                {
                    for (int i = 0; i < iColumnCount; i++)
                    {
                        DataColumn dataColumn = null;
                        if (ColNameList.Count > i && ColNameList[i].HasSet == 1)
                        {
                            if (string.IsNullOrEmpty(ColNameList[i].ColName))
                            {
                                dataColumn = new DataColumn($"{ColumnIDHelper.NumberToColumnID(i)}");
                            }
                            else
                            {
                                dataColumn = dataColumn = new DataColumn((ColumnIDHelper.NumberToColumnID(i) + " " + ColNameList[i].ColName).Trim());
                            }
                        }
                        else
                        {
                            dataColumn = string.IsNullOrEmpty(_ContentList[0][i]) ? new DataColumn(ColumnIDHelper.NumberToColumnID(i)) : new DataColumn((ColumnIDHelper.NumberToColumnID(i) + " " + _ContentList[0][i]).Trim());//如果原始数据列名后面有空格的话会影响列数据显示（该列内容不显示）
                        }
                        _DataTable.Columns.Add(dataColumn);
                    }
                }
            }
            

            for (int i = 1; i < _ContentList.Count; i++)
            {
                DataRow dr = _DataTable.NewRow(); //字段对象
                for (int j = 0; j < iColumnCount; j++)
                {
                    string _Value = string.Empty;

                    if (_ContentList[i].Count > j)
                    {
                        _Value = _ContentList[i][j];
                    }
                    
                    dr[j] = _Value;
                }
                _DataTable.Rows.Add(dr); //添加给表
            }
        }

        public static string get_uft8(string unicodeString)
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            Byte[] encodedBytes = utf8.GetBytes(unicodeString);
            String decodedString = utf8.GetString(encodedBytes);
            return decodedString;
        }

        /// <summary>
        /// 检测空行
        /// </summary>
        /// <param name="_RowContentList"></param>
        /// <returns></returns>
        public static bool VerifyEmptyRow(List<string> _RowContentList)
        {
            bool bRet = true;
            foreach (var item in _RowContentList)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    bRet = false;
                    break;
                }
            }
            return bRet;
        }

        /// <summary>
        /// 判断传入的列数据（字符串集合）是否为空 暂时不需要增加此逻辑
        /// </summary>
        /// <param name="_ColumnContentList"></param>
        /// <returns></returns>
        private static bool VerifyEmptyColumn(List<string> _ColumnContentList)
        {
            bool bRet = true;
            foreach (var item in _ColumnContentList)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    bRet = false;
                    break;
                }
            }
            return bRet;
        }

        /// <summary>
        /// 去除空列的后半段 暂时不需要增加此逻辑
        /// </summary>
        /// <param name="_ColumnContentList"></param>
        /// <returns></returns>
        private static bool RegenerateColumnContentList(List<string> _ColumnContentList)
        {
            bool bRet = true;
            for (int i= _ColumnContentList.Count-1; i>=0;i--)
            {
                if (string.IsNullOrWhiteSpace(_ColumnContentList[i]))
                {
                    _ColumnContentList.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }
            return bRet;
        }

        #endregion

        /// <summary>
        /// 重新命名DataTable列名 弃用
        /// </summary>
        /// <param name="dataTable"></param>
        public static void ReGenerateColumnName(DataTable dataTable)
        {
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                string fullName = dataTable.Columns[i].ColumnName;
                string nameTail = String.Empty;
                if (fullName.Contains(" "))
                {
                    int Cindex = fullName.IndexOf(" ");
                    nameTail = fullName.Substring(Cindex + 1, fullName.Length - Cindex-1);
                }

                if (string.IsNullOrEmpty(nameTail))
                {
                    dataTable.Columns[i].ColumnName = i.ToString();  //ColumnIDHelper.NumberToColumnID(i);
                }
                else
                {
                    dataTable.Columns[i].ColumnName = i + " " + nameTail;  //ColumnIDHelper.NumberToColumnID(i) + " " + nameTail;
                }
            }

            for (int i=0; i< dataTable.Columns.Count;i++)
            {
                string fullName = dataTable.Columns[i].ColumnName;
                string nameTail = String.Empty;

                if (fullName.Contains(" "))
                {
                    int Cindex = fullName.IndexOf(" ");
                    nameTail = fullName.Substring(Cindex+1, fullName.Length- Cindex-1);
                }

                if (string.IsNullOrEmpty(nameTail))
                {
                    dataTable.Columns[i].ColumnName = ColumnIDHelper.NumberToColumnID(i);
                }
                else
                {
                    dataTable.Columns[i].ColumnName = ColumnIDHelper.NumberToColumnID(i)+" "+ nameTail;
                }
            }
        }


        /// <summary>
        /// 重新命名DataTable列名
        /// </summary>
        /// <param name="dataTable">目标DataTable</param>
        /// <param name="_NameList">列名List (含空值)</param>
        #region 重命名 表格列名
        public static void ReGenerateColumnNameEx(DataTable dataTable, List<string> _NameList)
        {
            if (dataTable.Columns.Count != _NameList.Count)
            {
                return;
            }

            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                dataTable.Columns[i].ColumnName = i.ToString();
            }

            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                if (string.IsNullOrEmpty(_NameList[i]))
                {
                    dataTable.Columns[i].ColumnName = ColumnIDHelper.NumberToColumnID(i);
                }
                else
                {
                    dataTable.Columns[i].ColumnName = ColumnIDHelper.NumberToColumnID(i) + " " + _NameList[i];
                }
            }
        }
        #endregion



        /// <summary>
        /// 重新命名配置信息里的列元素名称
        /// </summary>
        /// <param name="_DataCollectConfiguration_CollectInfo"></param>
        public static void RenameTitles(DataCollectConfiguration_CollectInfo _DataCollectConfiguration_CollectInfo)
        {
            for (int i = 0; i < _DataCollectConfiguration_CollectInfo.data.Count; i++)
            {
                string fullName = _DataCollectConfiguration_CollectInfo.data[i].title;
                string nameTail = String.Empty;
                if (fullName.Contains(" "))
                {
                    int Cindex = fullName.IndexOf(" ");
                    nameTail = fullName.Substring(Cindex + 1, fullName.Length - Cindex - 1);
                }

                if (string.IsNullOrEmpty(nameTail))
                {
                    _DataCollectConfiguration_CollectInfo.data[i].title = ColumnIDHelper.NumberToColumnID(i);
                }
                else
                {
                    _DataCollectConfiguration_CollectInfo.data[i].title = ColumnIDHelper.NumberToColumnID(i) + " " + nameTail;
                }
            }

            return;
        }


    }
    #endregion
}
