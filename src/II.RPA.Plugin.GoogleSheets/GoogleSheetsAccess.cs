using II.RPA.Plugin.GoogleSheets.SheetsWrapper;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets
{
    public class GoogleSheetsAccess
    {
        private SheetAdapter adapter;
        private string spreadIdTemp;
        /// <summary>
        /// 拷贝粘贴谷歌表
        /// </summary>
        /// <param name="url"></param>
        /// <param name="start">拷贝起始位置</param>
        /// <param name="end">拷贝结束位置</param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public  string CopyPasteSheet(string url, string sourceSheet,string sourceStart,string sourceEnd,string destSheet, string destStart, string destEnd,bool onlyValue)
        {
            try
            {
                SheetAdapter sheetHelper = GetAdapter(url);
                sheetHelper.CopyPaste(sourceSheet, destSheet, sourceStart, sourceEnd, destStart, destEnd,onlyValue);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
            return string.Empty;
        }
        /// <summary>
        /// 选中区域
        /// </summary>
        /// <param name="url"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public string SelectArea(string url,string start,string end,string sheetName)
        {
            return string.Empty;
        }

        public string RenameSheetName(string url,string oldName,string newName)
        {
            try
            {
                SheetAdapter sheetHelper = GetAdapter(url);
                sheetHelper.UpdateSheetName(oldName, newName);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return string.Empty;
        }
        
        public string GetActivedSheetName(string url)
        {
            return string.Empty;
        }
        public string AddNewSheet(string url ,string sheetName)
        {
            try
            {
                SheetAdapter sheetHelper = GetAdapter(url);
                sheetHelper.AddNewSheet(sheetName);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return string.Empty;
        }
        
        public string SelectSheet(string url,string sheetName)
        {
            try
            {
                SheetAdapter sheetHelper = GetAdapter(url);
                sheetHelper.AddNewSheet(sheetName);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return string.Empty;
        }
       
        public string DeleteSheet(string url,string sheetName)
        {
            try
            {
                SheetAdapter sheetHelper = GetAdapter(url);
                sheetHelper.DeleteSheet(sheetName);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return string.Empty;
        }
        /// <summary>
        /// 返回所有sheet表名
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetAllSheetsName(string url)
        {
            string strSheets = string.Empty;
            try
            {
                SheetAdapter sheetHelper = GetAdapter(url);
                List<string> sheets= sheetHelper.GetAllTabNames();
                strSheets = JsonHelper.JsonByNewtonJson(sheets);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return strSheets;
        }
        /// <summary>
        /// 获取列上第一个可用行
        /// </summary>
        /// <param name="url"></param>
        /// <param name="columnName"></param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public string GetFirstCanUseRow(string url,string columnName,string sheetName)
        {
            return string.Empty;
        }
        /// <summary>
        /// 读取谷歌表内容
        /// </summary>
        /// <param name="url"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public string GetAllData(string url,string start,string end,string sheetName)
        {
            string strSheets = string.Empty;
            try
            {
                SheetAdapter sheetHelper = GetAdapter(url);
                string range = string.Format("{0}!{1}:{2}", sheetName, start, end);
                SheetRange sr = new SheetRange(range);
                var objs = sheetHelper.GetRows(sr);
                strSheets = JsonHelper.JsonByNewtonJson(objs);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return strSheets;
        }
        /// <summary>
        /// 写入内容至谷歌表单元格
        /// </summary>
        /// <param name="url"></param>
        /// <param name="location"></param>
        /// <param name="content"></param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public string WriteToSheetCell(string url,string location,string content,string sheetName)
        {
            return string.Empty;
        }
        /// <summary>
        /// 写入区域
        /// </summary>
        /// <param name="url"></param>
        /// <param name="location"></param>
        /// <param name="mode">0：行，1：列，2：区域</param>
        /// <param name="content"></param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public string WriteToSheetRegion(string url,string location,int mode,string content,string sheetName)
        {
            return string.Empty;
        }

        public string NewFilterView(string url,string sheetName)
        {
            string strSheets = string.Empty;
            try
            {
                SheetAdapter sheetHelper = GetAdapter(url);
                 sheetHelper.AddFilterView();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return strSheets;
        }
        public string  InsertHylink(string url,string cell,string content,string hylink,string sheetName)
        {
            return string.Empty;
        }
       

        private string GetSpreadId(string url)
        {
            string prifi = "https://docs.google.com/spreadsheets/d/";
            string endfi = @"/edit#";
            if(!url.StartsWith(prifi)||!url.Contains(endfi))
            {
                throw new Exception("Invalid url,please check");
            }
            string str = url.Replace(prifi, "");
            return str.Substring(0, str.IndexOf(endfi));
        }
       
        private SheetAdapter GetAdapter(string url)
        {
            try
            {
                string spreadId = GetSpreadId(url);
                if (spreadIdTemp != spreadId)
                {
                    adapter = new SheetAdapter(spreadId, "", "");
                    spreadIdTemp = spreadId;
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return adapter;
        }
    }
}
