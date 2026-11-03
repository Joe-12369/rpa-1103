using II.RPA.Plugin.Excel.Config;
using II.RPA.Plugin.Excel.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.Excel
{

    /// <summary>
    /// 组件执行上下文
    /// </summary>
    public class ActionContext
    {
        public ActionContext()
        {
            RequestId = DateTime.Now.Ticks;
        }
        private ExcelEditRange excelUsedRange;
        /// <summary>
        /// 表格当前编辑区域
        /// </summary>
        public ExcelEditRange ExcelUsedRange
        {
            get
            {
                if (!excelUsedRange.HasValue)
                {
                    excelUsedRange = excelUsedRange.GetExcelRange(this);
                }
                return excelUsedRange;
            }
            private set { }
        }


        /// <summary>
        /// Excel 实例
        /// </summary>
        public IExcel ExcelInstance { get; set; }

        public bool? SingleRun { get; set; }

        /// <summary>
        /// 组件执行编号
        /// </summary>
        public long RequestId { get; private set; }

        /// <summary>
        /// 执行超时时间
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// 执行前延时，单位毫秒
        /// </summary>
        public int? DelayBefore { get; set; }

        /// <summary>
        /// 组件名
        /// </summary>
        public string ActionName { get; set; }
        /// <summary>
        /// 组件描述
        /// </summary>
        public string ActionDescription
        {
            get
            {
                try
                {
                    if (ActionParams.TryGetValue("__redirectAction", out dynamic redirectName))
                    {
                        return (string)ExcelConfigurations.Info.ActionSetting[redirectName]["name"];
                    }
                    else
                    {
                        if (ExcelConfigurations.Info.ActionSetting.TryGetValue(ActionName, out JObject config))
                        {
                            return (string)config["name"];
                        }
                        return ActionName;
                    }
                }
                catch (System.Exception ex)
                {
                    ExcelPlugin.logger.LogError(ex);
                    return ActionName;
                }

            }
        }
        public dynamic GetActionArgs(string name)
        {
            try
            {
                var valObj = ExcelConfigurations.Info.ActionSetting[ActionName];
                var value = valObj.GetValue(name, StringComparison.OrdinalIgnoreCase)?.Value<string>();
                ExcelPlugin.logger.LogInformation($"{ActionName} {name}:{value}");
                return value;
            }
            catch (System.Exception ex)
            {
                ExcelPlugin.logger.LogError(ex);
                return null;
            }
        }
        /// <summary>
        /// 组件执行参数
        /// </summary>
        public Dictionary<string, dynamic> ActionParams { get; set; }

        public dynamic this[string key]
        {
            get
            {
                if (ActionParams.TryGetValue(key, out dynamic _value))
                {
                    return _value;
                }
                else
                {
                    return null;
                }

            }
            set
            {
                if (ActionParams.ContainsKey(key))
                {
                    ActionParams[key] = value;
                }
                else
                {
                    ActionParams.Add(key, value);
                }

            }
        }
        /// <summary>
        /// 组件执行异常信息
        /// </summary>
        public Exception Exception { get; set; }
        /// <summary>
        /// 组件执行结果
        /// </summary>
        public JObject Result { get; set; } = new JObject();
        /// <summary>
        /// 组件执行返回结果信息
        /// </summary>
        /// <param name="property"></param>
        /// <param name="token"></param>
        public void AddResult(string property, JToken token)
        {
            if (Result.ContainsKey(property))
            {
                Result.Remove(property);
            }
            Result.Add(property, token);
        }
        /// <summary>
        /// 组件执行返回原始字符串，非Json
        /// </summary>
        /// <param name="rawString"></param>
        public void AddResult(JToken rawString)
        {
            AddResult("Raw_String", rawString);
        }
        internal void AddResult(JProperty jProperty)
        {
            Result.Add(jProperty);
        }
    }

    public struct ExcelEditRange
    {
        public int StartRow { get; private set; }
        public int EndRow { get; private set; }
        public string StartColumn { get; private set; }
        public string EndColumn { get; private set; }
        public int EndColumnIndex { get; private set; }
        /// <summary>
        /// 已编辑区域得地址
        /// </summary>
        public string Address
        {
            get
            {
                return $"{StartColumn}{StartRow}:{EndColumn}{EndRow}";
            }
        }

        public bool HasValue { get; private set; }
        public ExcelEditRange GetExcelRange(ActionContext context)
        {
            context.ExcelInstance.GetEditRange(Convert.ToString(context["workbook"]), Convert.ToString(context["sheet"]));
            EndRow = context.Result.Value<int>("endRowNum");
            EndColumn = context.Result.Value<string>("endColumn");
            StartRow = context.Result.Value<int>("startRowNum");
            StartColumn = context.Result.Value<string>("startColumn");
            EndColumnIndex = ExcelUtils.ColumnLetterToIndex(EndColumn);
            HasValue = true;
            return this;

        }
    }
}
