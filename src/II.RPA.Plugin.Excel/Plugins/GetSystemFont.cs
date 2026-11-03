using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Excel.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Excel.Plugins
{
    public class GetSystemFont : PluginAbilityBase<ParamGetSystemFont>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ExecuteExcel;

        protected override JMessage Execute(ParamGetSystemFont paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();

            try
            {
                List<string> systemFonts = new List<string>();
                try
                {
                   systemFonts = GetSystemFonts();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {

                }

                msg.Info = systemFonts.ToJson();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "GetSystemFont ERR");
                Exception(ex, eErrorMessageType.Error_ActiveWindow, ref msg);
            }

            return msg;
        }

        protected override void ValidateParameter(ParamGetSystemFont paramModel) { }

        public List<string> GetSystemFonts()
        {
            List<string> FontList = new List<string>();

            // 获取系统所有可用字体族  
            System.Drawing.FontFamily[] fontFamilies = System.Drawing.FontFamily.Families;

            // 遍历并输出每个字体族的名字  
            foreach (System.Drawing.FontFamily fontFamily in fontFamilies)
            {
                FontList.Add(fontFamily.Name);
            }

            FontList.Sort(new StringCNUSComparer());
            return FontList;
        }

        public class StringCNUSComparer : IComparer<string>
        {
            public int Compare(string A, string B)
            {
                int iStringA = IsCNText(A) ? 1 : 0;
                int iStringB = IsCNText(B) ? 1 : 0;

                return iStringB - iStringA;
            }

            public bool IsCNText(string text)
            {
                string pattern = @"[\u4e00-\u9fa5]+";
                Regex regex = new Regex(pattern);
                return regex.IsMatch(text);
            }
        }


    }
}

