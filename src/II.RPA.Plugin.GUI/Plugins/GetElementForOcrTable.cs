using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Logging.Formatting;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetElementForOcrTable : GuiComponentBase<ParamGetElementForOcrTableModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.OcrTableAnalyse;

        protected override void ValidateParameter(ParamGetElementForOcrTableModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"GetCVText 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamGetElementForOcrTableModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                logger.LogInformation($"GetElementForOcrTable 获取selector:{paramModel.Selector}");
                GuiElement _ButtinElement = GuiElement.FromSelector(paramModel.Selector, paramModel.SelectorType, false, "", null, true, true, true);//最后一个参数等同于置顶窗体
                if (_ButtinElement == null)
                {
                    Exception(null, eErrorMessageType.Error_NotFoundElement, ref msg);
                    logger.LogError($"获取表格解析失败");
                    return msg;
                }
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                FindElementResult _findElementResult = new FindElementResult();
                _findElementResult.ElementFound = true;
                _findElementResult.WindowX = _ButtinElement.BoundingRectangle.X;
                _findElementResult.WindowY = _ButtinElement.BoundingRectangle.Y;
                _findElementResult.WindowWidth = _ButtinElement.BoundingRectangle.Width;
                _findElementResult.WindowHeight = _ButtinElement.BoundingRectangle.Height;
                msg.Info = _findElementResult.ToJson();
                logger.LogInformation($"GetElementForOcrTable 准备输出content {LogBlock.Object(msg)}");
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetTextError, ref msg);
            }
            return msg;
        }

    }

    public class FindElementResult
    {
        public bool ElementFound { get; set; } = false;
        public int WindowX { get; set; }
        public int WindowY { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
    }
}

