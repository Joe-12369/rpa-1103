using II.RPA.Plugin.Dialog.UserControls;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Controls;

namespace II.RPA.Plugin.Dialog
{
    public class ExcelHelper
    {
        private static ILogger logger = Logger.OfContext();
        private const int MAX_COLUMN_COUNT = 100;
        private const int MIN_COLUMN_COUNT = 5;
        private const int MIN_ROW_COUNT = 3;
        private static readonly string[] _columnNames = new string[] {
            "A","B","C","D","E","F","G","H","I","J",
            "K","L","M","N","O","P","Q","R","S","T",
            "U","V","W","X","Y","Z",
            "AA","AB","AC","AD","AE","AF","AG","AH","AI","AJ",
            "AK","AL","AM","AN","AO","AP","AQ","AR","AS","AT",
            "AU","AV","AW","AX","AY","AZ",
            "BA","BB","BC","BD","BE","BF","BG","BH","BI","BJ",
            "BK","BL","BM","BN","BO","BP","BQ","BR","BS","BT",
            "BU","BV","BW","BX","BY","BZ",
            "CA","CB","CC","CD","CE","CF","CG","CH","CI","CJ",
            "CK","CL","CM","CN","CO","CP","CQ","CR","CS","CT",
            "CU","CV",
        };

        public static List<TabItem> ImportSheetData(string value)
        {
            DataTable dataTable = null;
            List<TabItem> listTab = new List<TabItem>();
            var sheetDatas = JsonConvert.DeserializeObject<List<SheetData>>(value);
            foreach (var sheet in sheetDatas)
            {
                TabItem tabItem = new TabItem();
                tabItem.Header = sheet.SheetName;
                dataTable = DataTableBuilder(sheet.Data);
                SingletonDataTable singletonDataTable = new SingletonDataTable();
                singletonDataTable.TABLE.ItemsSource = dataTable.DefaultView;
                tabItem.Content = singletonDataTable;
                listTab.Add(tabItem);
            }

            return listTab;
        }
        private static DataTable DataTableBuilder(string[,] source)
        {
            DataTable dt = new DataTable();
            try
            {
                int height = source.GetLength(0);
                int width = source.GetLength(1);

                for (int w = 0; w < width; w++)
                {
                    DataColumn column = new DataColumn(_columnNames[w]);
                    dt.Columns.Add(column);
                }
                for (int h = 0; h < height; h++)
                {
                    DataRow row = dt.NewRow();
                    for (int w = 0; w < width; w++)
                    {
                        row[w] = source[h, w];
                    }
                    dt.Rows.Add(row);
                }

            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "Excel解析异常");
            }
            return dt;
        }

        public static bool IsCanDeleteRow(int rowCount)
        {
            return rowCount > MIN_ROW_COUNT;
        }

        public static IEnumerable<string> GetNames(string filePath, out ExcelReadMessage errInfo)
        {
            IList<string> list = new List<string>();
            errInfo = new ExcelReadMessage();

            //1.判断文件是否存在
            if (!File.Exists(filePath))
            {
                errInfo.SetMessage(0);
                return list;
            }
            FileStream fs;
            //2.判断文件是否被占用
            try
            {
                fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (Exception ex)
            {
                errInfo.SetMessage(1);
                return list;
            }
            IWorkbook workbook;
            //3.判断文件格式是否正确
            try
            {
                if (filePath.IndexOf(".xlsx") > 0) // 2007版本
                {
                    workbook = new XSSFWorkbook(fs);
                }
                else if (filePath.IndexOf(".xls") > 0) // 2003版本
                {
                    workbook = new HSSFWorkbook(fs);
                }
                else
                {
                    errInfo.SetMessage(2);
                    return list;
                }
            }
            catch (Exception)
            {
                errInfo.SetMessage(2);
                return list;
            }

            int count = workbook.NumberOfSheets;
            for (int i = 0; i < count; i++)
            {
                list.Add(workbook.GetSheetName(i));
            }
            return list;
        }

        public static DataTable ImportData(string filePath, out ExcelReadMessage errInfo)
        {
            DataTable data = new DataTable();
            errInfo = new ExcelReadMessage();
            try
            {
                //1.判断文件是否存在
                if (!File.Exists(filePath))
                {
                    errInfo.SetMessage(0);
                    return data;
                }
                FileStream fs;
                //2.判断文件是否被占用
                try
                {
                    fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }
                catch (Exception)
                {
                    errInfo.SetMessage(1);
                    return data;
                }
                IWorkbook workbook;
                //3.判断文件格式是否正确
                if (filePath.IndexOf(".xlsx") > 0) // 2007版本
                {
                    workbook = new XSSFWorkbook(fs);
                }
                else if (filePath.IndexOf(".xls") > 0) // 2003版本
                {
                    workbook = new HSSFWorkbook(fs);
                }
                else
                {
                    errInfo.SetMessage(2);
                    return data;
                }
                ISheet sheet = workbook.GetSheetAt(0);

                if (sheet != null)
                {
                    int maxLength = 0;
                    foreach (IRow row in sheet)
                    {
                        maxLength = maxLength > row.LastCellNum ? maxLength : row.LastCellNum;
                    }
                    //4.判断是否超出设定值
                    if (maxLength > MAX_COLUMN_COUNT)
                    {
                        errInfo.SetMessage(3);
                        return data;
                    }
                    for (int i = 0; i < maxLength; i++)
                    {
                        DataColumn column = new DataColumn(_columnNames[i]);
                        data.Columns.Add(column);
                    }

                    //最后一列的标号
                    int rowCount = sheet.LastRowNum;
                    for (int i = 0; i <= rowCount; ++i)
                    {
                        IRow row = sheet.GetRow(i);
                        DataRow dataRow = data.NewRow();
                        if (row != null)
                        {
                            for (int j = row.FirstCellNum; j < row.LastCellNum; ++j)
                            {
                                if (row.GetCell(j) != null)
                                {
                                    dataRow[j] = row.GetCell(j).ToString();
                                }
                            }
                        }
                        data.Rows.Add(dataRow);
                    }
                }
            }
            catch (Exception ex)
            {
                errInfo.SetMessage(ex);
            }
            BuildDataTable(data);
            return data;
        }


        public static DataTable ImportData(string filePath, string sheetName, out ExcelReadMessage errInfo)
        {
            DataTable data = new DataTable();
            errInfo = new ExcelReadMessage();
            try
            {
                //1.判断文件是否存在
                if (!File.Exists(filePath))
                {
                    errInfo.SetMessage(0);
                    return data;
                }
                FileStream fs;
                //2.判断文件是否被占用
                try
                {
                    fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }
                catch (Exception)
                {
                    errInfo.SetMessage(1);
                    return data;
                }
                IWorkbook workbook;
                //3.判断文件格式是否正确
                if (filePath.IndexOf(".xlsx") > 0) // 2007版本
                {
                    workbook = new XSSFWorkbook(fs);
                }
                else if (filePath.IndexOf(".xls") > 0) // 2003版本
                {
                    workbook = new HSSFWorkbook(fs);
                }
                else
                {
                    errInfo.SetMessage(2);
                    return data;
                }
                ISheet sheet;
                if (string.IsNullOrEmpty(sheetName))
                {
                    sheet = workbook.GetSheetAt(0);
                }
                else
                {
                    sheet = workbook.GetSheet(sheetName);
                }

                if (sheet != null)
                {
                    int maxLength = 0;
                    foreach (IRow row in sheet)
                    {
                        maxLength = maxLength > row.LastCellNum ? maxLength : row.LastCellNum;
                    }
                    //4.判断是否超出设定值
                    if (maxLength > MAX_COLUMN_COUNT)
                    {
                        errInfo.SetMessage(3);
                        return data;
                    }
                    for (int i = 0; i < maxLength; i++)
                    {
                        DataColumn column = new DataColumn(_columnNames[i]);
                        data.Columns.Add(column);
                    }

                    //最后一列的标号
                    int rowCount = sheet.LastRowNum;
                    for (int i = 0; i <= rowCount; ++i)
                    {
                        IRow row = sheet.GetRow(i);
                        DataRow dataRow = data.NewRow();
                        if (row != null)
                        {
                            for (int j = row.FirstCellNum; j < row.LastCellNum; ++j)
                            {
                                if (row.GetCell(j) != null)
                                {
                                    dataRow[j] = row.GetCell(j).ToString();
                                }
                            }
                        }
                        data.Rows.Add(dataRow);
                    }
                }
            }
            catch (Exception ex)
            {
                errInfo.SetMessage(ex);
            }
            BuildDataTable(data);
            return data;
        }
        internal static string GetColumnName(int index)
        {
            if (index >= MAX_COLUMN_COUNT)
            {
                throw new Exception(eCommonMessageType.ExcelHelper_cs_LieChaoChuSheDingDeZuiDaZhi.GetMessage());
            }
            return _columnNames[index];
        }

        internal static void DeleteColumn(DataTable dt, int index)
        {
            if (dt.Columns.Count <= MIN_COLUMN_COUNT)
            {
                throw new Exception(eCommonMessageType.ExcelHelper_cs_LieBuNengXiaoYuSheDingDeZuiXiao.GetMessage());
            }
            dt.Columns.RemoveAt(index);
            for (int i = index; i < dt.Columns.Count; i++)
            {
                dt.Columns[i].ColumnName = _columnNames[i];
            }
        }

        private static void BuildDataTable(DataTable dt)
        {
            if (dt.Columns.Count < MIN_COLUMN_COUNT)
            {
                for (int i = dt.Columns.Count; i < MIN_COLUMN_COUNT; i++)
                {
                    dt.Columns.Add(new DataColumn(_columnNames[i]));
                }
            }
            if (dt.Rows.Count < 4)
            {
                for (int i = dt.Rows.Count; i < MIN_ROW_COUNT; i++)
                {
                    var dr = dt.NewRow();
                    dt.Rows.Add(dr);
                }
            }
        }

        public static void ExportData(DataTable dt, string filePath, out ExcelWriteMessage message)
        {
            message = new ExcelWriteMessage();
            IWorkbook workbook;

            FileStream fs;
            try
            {
                if (!File.Exists(filePath))
                {
                    fs = File.Create(filePath);
                }
                else
                {
                    fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                }
                if (filePath.IndexOf(".xlsx") > 0)
                {
                    workbook = new XSSFWorkbook();
                }
                else if (filePath.IndexOf(".xls") > 0)
                {
                    workbook = new HSSFWorkbook();
                }
                else
                {
                    message.SetMessage(0);
                    return;
                }
                ISheet sheet = workbook.CreateSheet();
                if (sheet == null)
                {
                    message.SetMessage(1);
                    return;
                }
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    IRow row = sheet.CreateRow(i);
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        row.CreateCell(j).SetCellValue(dt.Rows[i][j].ToString());
                    }
                }
                workbook.Write(fs);
                fs.Close();
                workbook.Close();
            }
            catch (Exception ex)
            {
                message.SetMessage(ex);
            }
        }
    }

    public class ExcelReadMessage
    {
        private readonly string[] importMessage = new string[] {
            eCommonMessageType.ChromeExtensionProvider_cs_WenJianBuCunZai.GetMessage(),
            eCommonMessageType.ExcelHelper_cs_WenJianBeiZhanYongQingGuanBiHou.GetMessage(),
            eCommonMessageType.ExcelHelper_cs_WenJianBuShiBiaoZhunExce.GetMessage(),
            eCommonMessageType.ExcelHelper_cs_LieChaoChuSheDingDeZuiDaZhi.GetMessage()
        };
        public bool Code { get; set; } = true;
        public string Message { get; set; }
        public void SetMessage(int messageCode)
        {
            Code = false;
            Message = importMessage[messageCode];
        }

        public void SetMessage(Exception ex)
        {
            Code = false;
            Message = eCommonMessageType.ExcelHelper_cs_WeiZhiDeYiChang.GetMessage() + ex.Message;
        }
    }

    public class ExcelWriteMessage
    {
        private readonly string[] exportMessage = new string[] {
           eCommonMessageType.ExcelHelper_cs_WenJianHouZhuiMingYouWu.GetMessage(),
           eCommonMessageType.ExcelHelper_cs_ChuangJianSheetShiBai.GetMessage() ,
        };
        public bool Code { get; set; } = true;
        public string Message { get; set; }
        public void SetMessage(int index)
        {
            Code = false;
            Message = exportMessage[index];
        }
        public void SetMessage(Exception ex)
        {
            Code = false;
            Message = eCommonMessageType.ExcelHelper_cs_WeiZhiDeYiChang.GetMessage() + ex.Message;
        }
    }
}
