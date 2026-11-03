using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.FacDialog
{
    public class WindowDialog
    {

        private static ILogger logger = Logger.OfContext();
        private ParamShowWindowDialogModel windowDialogParams = null;
        public WindowDialog(ParamShowWindowDialogModel dialogParams)
        {
            windowDialogParams = dialogParams;
        }
        public WindowDialog(ParamShowWindowDialogModel dialogParams, params ButtonType[] buttonTypes)
        {
            windowDialogParams = dialogParams;
            AddButtonType(buttonTypes);
        }
        public ButtonType ShowDialog()
        {
            windowDialogParams.Buttons = windowDialogParams.Buttons ?? new List<ButtonsItem>();
            List<ButtonsItem> btns = this.GetButtons();
            foreach (var btn in btns)
            {
                if (this.windowDialogParams.Buttons.IndexOf(btn) < 0)
                {
                    this.windowDialogParams.Buttons.Add(btn);
                }
            }
            string dialogRet = FacMessageHelper.ShowWindowDialog(windowDialogParams);
            JObject retObj = JObject.Parse(dialogRet);
            if (retObj.ContainsKey("eventId") && Enum.TryParse<ButtonType>(retObj["eventId"].ToString(), out var btnType))
            {
                return btnType;
            }
            else {
                logger.LogDebug($"dialog result ：{dialogRet}");
                return ButtonType.WindowClose;
            }
        }

        #region 按钮 属性

        [JsonIgnore]
        private List<ButtonType> ButtonTypes { get; set; } = new List<ButtonType>();

        private List<ButtonsItem> GetButtons()
        {
            return ButtonTypes.Select(t =>
            {
                string name = t.ToString();
                string text = name;
                FieldInfo fieldInfo = t.GetType().GetField(text);
                ButtonPropertyAttribute buttonPropertyAttr = fieldInfo.GetCustomAttributes(typeof(ButtonPropertyAttribute), false).FirstOrDefault() as ButtonPropertyAttribute;
                if (buttonPropertyAttr != null)
                {
                    text = buttonPropertyAttr.Text;
                }

                return new ButtonsItem
                {
                    Text = text,
                    IsNormal = !buttonPropertyAttr.Colored,
                    EventId = name
                };
            }).ToList();
        }

        #endregion
        #region 增加按钮
        public void AddButtonType(params ButtonType[] buttonTypes)
        {
            foreach (var btnType in buttonTypes)
            {
                if (this.ButtonTypes.IndexOf(btnType) < 0)
                {
                    this.ButtonTypes.Add(btnType);
                }
            }
        }
        #endregion


    }
}
