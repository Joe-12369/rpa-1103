using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Core;
using II.RPA.Plugin.Window.Params;
using II.RPA.WinAPI;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;

namespace II.RPA.Plugin.Window.Plugins
{
    public class GetWindowList : WindowComponentBase<ParamGetWindowListModel>
    {
        protected override eComponentType ComponentType => eComponentType.GetWindowList;

        protected override JMessage Execute(ParamGetWindowListModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            var excludedNames = new[] { "实在设计器", "Z-Factory","Z-Bot", "Program Manager", "MainForm" };
            JMessage msg = new JMessage();

            try
            {
                var list = WindowOperator.ListAllWindows();
                var items = list
                    .Select(x => x.Title)
                    .Except(excludedNames)
                    .ToList();

                msg.Info = JsonConvert.SerializeObject(items);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetWindowList, ref msg);
            }

            return msg;
        }

        protected override void ValidateParameter(ParamGetWindowListModel paramModel)
        {
        }
    }
}
