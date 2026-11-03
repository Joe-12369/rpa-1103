using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Core;
using II.RPA.Plugin.Window.Params;
using II.RPA.WinAPI;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Automation;

namespace II.RPA.Plugin.Window.Plugins
{
    public class GetWindowDataList : WindowComponentBase<ParamGetWindowDataListModel>
    {
        protected override eComponentType ComponentType => eComponentType.GetWindowDataList;
        protected override JMessage Execute(ParamGetWindowDataListModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                List<WindowData> datas = WindowOperator.FindByKeywords(paramModel.Keywords, paramModel.MatchType);
                msg.Info = datas.Select(data=>data.ToJson()).ToList().ToJson();
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetWindowData, ref msg);
            }

            return msg;
        }
    }
}
