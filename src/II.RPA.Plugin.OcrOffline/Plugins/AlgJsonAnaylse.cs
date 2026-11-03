using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OcrOffline.Plugins
{

    internal class AlgJsonAnaylse
    {
        public static bool HasData(string strJsonResult, ref string errMsg)
        {
            bool bHasData = false;
            errMsg = string.Empty;

            try
            {
                var obj = JsonConvert.DeserializeObject<JToken>(strJsonResult);

                if (!string.IsNullOrEmpty(obj?["status"]?.ToString()))
                {
                    bHasData = (obj?["status"]?.ToString().ToLower() == ("success"));
                }

                if (!bHasData)
                {
                    if (!string.IsNullOrEmpty(obj?["message"]?.ToString()))
                    {
                        errMsg = obj?["message"]?.ToString();
                    }
                    return bHasData;
                }

                if (!string.IsNullOrEmpty(obj?["result"]?.ToString()))
                {
                    JToken objResult = JsonConvert.DeserializeObject<JToken>(obj?["result"]?.ToString());

                    if (string.IsNullOrEmpty(objResult?["table_structure"]?.ToString()))
                    {
                        return bHasData;
                    }

                    JToken table_structure = JsonConvert.DeserializeObject<JToken>(objResult?["table_structure"]?.ToString());

                    if (string.IsNullOrEmpty(table_structure?["table_count"]?.ToString()))
                    {
                        return bHasData;
                    }

                    int TableCount = Convert.ToInt32(table_structure?["table_count"]?.ToString());
                    bHasData = TableCount > 0;
                }
                else
                {
                    return bHasData;
                }
            }
            catch (Exception ex)
            {
                errMsg = Localize($"ExecuteCV 返回值解析失败:{ex.ToString()}", "AlgJsonAnaylse_8CDF990");
            }
            return bHasData;
        }
    }


}
