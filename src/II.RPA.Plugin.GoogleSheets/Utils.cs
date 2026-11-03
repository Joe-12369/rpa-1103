using II.RPA.Plugin.GoogleSheets.SheetsWrapper;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets
{
    class Utils
    {
        private static SheetAdapter adapter;
        private static string spreadIdTemp;
        public static bool IsValidGoogleSheetUrl(string url)
        {
            string prifi = "https://docs.google.com/spreadsheets/d/";
            string endfi = @"/edit#";
            string endfi_new = @"/edit?";
            if (!url.StartsWith(prifi) || !(url.Contains(endfi) || url.Contains(endfi_new)))
            {
                throw new Exception("Invalid url,please check");
            }
            return true;
        }
        public static string GetSpreadId(string url)
        {

            //https://docs.google.com/spreadsheets/d/1mJNS8wc8VGbSvlXWpmvbMbo8KONz6PXJmatEf8HeWWA/edit?gid=657942423#gid=657942423 new
            //https://docs.google.com/spreadsheets/d/1Mv-VXDqz9e6izELTlc-gDqUidn0z3QfCO11uegmkLYg/edit#gid=1453418220 old
            string prifi = "https://docs.google.com/spreadsheets/d/";
            string endfi = @"/edit#";
            string endfi_new = @"/edit?";
            if (!url.StartsWith(prifi) || !(url.Contains(endfi) || url.Contains(endfi_new)))
            {
                throw new Exception("Invalid url,please check");
            }
            string str = url.Replace(prifi, "");
            if (url.Contains(endfi))
            {
                return str.Substring(0, str.IndexOf(endfi));
            }
            else if (url.Contains(endfi_new))
            {
                return str.Substring(0, str.IndexOf(endfi_new));
            }
            else
            {
                throw new Exception("Url Path format exception");
            }
        }
        public static SheetAdapter GetAdapter(string url)
        {
            try
            {
                string spreadId = GetSpreadId(url);
                if (spreadIdTemp != spreadId)
                {
                    adapter = new SheetAdapter(spreadId, "", "");
                    string json = string.Empty;
                    var sheetJsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"GoogleTable\sheets.json");

                    using (FileStream fs = File.Open(sheetJsonPath, FileMode.Open))
                    {
                        byte[] bytes = new byte[fs.Length];
                        fs.ReadExactly(bytes);
                        fs.Flush();
                        json = Encoding.UTF8.GetString(bytes);
                    }
                    adapter.Init(json);
                    spreadIdTemp = spreadId;
                }
            }
            catch (Exception ex)
            {
                NLogger.Default.Error(ex);
                throw ex;
            }
            return adapter;
        }
    }
}
