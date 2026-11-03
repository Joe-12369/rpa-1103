using FlaUI.Core.Input;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;

namespace II.RPA.JavaBridgeHost
{
    public class JavaExecutor
    {
        private static ILogger logger = Logger.OfContext();
        const int SUCCESS_STATUS = 0;
        const int FAIL_STATUS = 1;
        const int CANCEL_STATUS = -1;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool SetProcessDPIAware();
        public JavaExecutor()
        {
            try
            {
                SetProcessDPIAware();
                //因为有延时
                JavaNode.RefreshJvms();

            }
            catch { }
        }
        public string GetTableData(ISelectorContext selectorData)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;
            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.ElementEnable;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.ElementEnable, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                msg.info = element.GetTableData();
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.ElementEnable;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_ElementIsEnable, eComponentType.ElementEnable, e);
                return GetEncodingStr(rpaException);
            }
            //element.Dispose();
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string GetButtonEnable(ISelectorContext selectorData, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;
            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.ElementEnable;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.ElementEnable, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                bool result = element.ButtonClickEnable();

                msg.info = result == true ? "1" : "0";
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.ElementEnable;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_ElementIsEnable, eComponentType.ElementEnable, e);
                return GetEncodingStr(rpaException);
            }
            //element.Dispose();
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string MouseHover(ISelectorContext selectorData, int offsetX, int offsetY, int cursorPos = 0, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.MouseHover;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.MouseHover, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                element.MouseHover(offsetX, offsetY, cursorPos);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.MouseHover;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_MoveElementError, eComponentType.MouseHover, e);
                return GetEncodingStr(rpaException);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string MouseClick(ISelectorContext selectorData, int clickType, int offsetX, int offsetY, bool issimulate, int cursorPos = 0, int keyType = 0, int timeout = 10000)
        {
            logger.LogInformation($"鼠标点击clickType:{clickType}keyType:{keyType}");
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;
            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)//原来的ChromeException
            {
                e.ComponentType = eComponentType.ElementClick;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                //构造一个RPAException，内部会打印日志、包装错误信息、错误码等
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.ElementClick, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                if (keyType == 0 && clickType == 0)
                {
                    element.MouseClick(offsetX, offsetY, issimulate, cursorPos);
                }
                else if (keyType == 0 && clickType == 1)
                {
                    element.MouseRightClick(offsetX, offsetY, cursorPos);
                }
                else if (keyType == 0 && clickType == 2)
                {
                    element.MouseConterClick(offsetX, offsetY, cursorPos);
                }
                else if (keyType == 1 && clickType == 0)
                {
                    element.MouseDoubleClick(offsetX, offsetY, cursorPos);
                }
                else if (keyType == 1 && clickType == 1)
                {
                    element.MouseRightDoubleClick(offsetX, offsetY, cursorPos);
                }
                else if (keyType == 1 && clickType == 2)
                {
                    element.MouseConterDoubleClick(offsetX, offsetY, cursorPos);
                }
                else if (keyType == 2 && clickType == 0)
                {
                    element.MouseThreeClick(offsetX, offsetY, cursorPos);
                }
                else if (keyType == 2 && clickType == 1)
                {
                    element.MouseRightThreeClick(offsetX, offsetY, cursorPos);
                }
                else if (keyType == 2 && clickType == 2)
                {
                    element.MouseConterThreeClick(offsetX, offsetY, cursorPos);
                }
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.ElementClick;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_ClickError, eComponentType.ElementClick, e);
                return GetEncodingStr(rpaException);
            }
            //element.Dispose();
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string SetText(ISelectorContext selectorData, string txt, int isReplace = 0, bool isSimulate = true, int timeout = 10000)
        {
            JMessage msg = new JMessage();

            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.SetText;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.SetText, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                element.SetText(txt, isReplace, isSimulate);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.SetText;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_SetTextError, eComponentType.SetText, e);
                return GetEncodingStr(rpaException);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        private string GetEncodingStr(RPAException _rpa_ex)
        {
            JMessage msg = new JMessage();
            msg.code = FAIL_STATUS;
            msg.message = _rpa_ex.ErrorMessage;
            if (msg.message.Contains("010400101240029"))
            {
                JObject ext = null;
                if (string.IsNullOrEmpty(msg.Extend))
                    ext = new JObject();
                else
                    ext = JObject.Parse(msg.Extend);
                ext.Add("ExtErrorCode", "010400101240029");
                msg.Extend = ext.ToString();
            }
            var encodeStr = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(encodeStr);
        }
        private string GetEncodingStr(string txt)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(txt);
            return Encoding.UTF8.GetString(bytes);
        }
        public string GetText(ISelectorContext selectorData, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;
            logger.LogDebug("Start GetText");
            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                LoggingExtensions.LogError(logger, e, "GetText RPAEx");
                e.ComponentType = eComponentType.GetText;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                LoggingExtensions.LogError(logger, ex, "GetText Ex");
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetText, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                string result = element.GetText(selectorData.Selector);
                msg.info = result;
            }
            catch (RPAException e)
            {
                LoggingExtensions.LogError(logger, e, "GetText RPAEx2");
                e.ComponentType = eComponentType.GetText;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                LoggingExtensions.LogError(logger, e, "GetText Ex2");
                var rpaException = new RPAException(eErrorMessageType.Error_GetTextError, eComponentType.GetText, e);
                return GetEncodingStr(rpaException);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string GetElementSelect(ISelectorContext selectorData, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;

            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.GetElementSelect;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetElementSelect, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                string result = element.GetElementSelect();
                msg.info = result;
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.GetElementSelect;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetElementSelect, e);
                return GetEncodingStr(rpaException);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }

        public string GetElementSelectEx(ISelectorContext selectorData, string strJson, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;

            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.GetElementSelect;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetElementSelect, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                string result = element.GetElementSelectEx(strJson);
                msg.info = result;
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.GetElementSelect;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetElementSelect, e);
                return GetEncodingStr(rpaException);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string GetElementCheck(ISelectorContext selectorData, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;

            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.GetElementCheck;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetElementCheck, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                string result = element.GetElementCheck();

                msg.info = result == "true" ? "1" : "0";
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.GetElementCheck;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_GetElementCheckError, eComponentType.GetElementCheck, e);
                return GetEncodingStr(rpaException);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string SetElementSelect(ISelectorContext selectorData, int selectType, string setValue, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;

            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.SetElementSelect;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.SetElementSelect, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                element.SetElementSelect(selectType, setValue);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.SetElementSelect;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_SetSelectError, eComponentType.SetElementSelect, e);
                return GetEncodingStr(rpaException);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string SetElementCheck(ISelectorContext selectorData, int check, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;

            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.SetElementCheck;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.SetElementCheck, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                element.SetElementCheck(check);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.SetElementCheck;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_SetElementCheckError, eComponentType.SetElementCheck, e);
                return GetEncodingStr(rpaException);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string GetRegion(ISelectorContext selectorData, int positionType, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;

            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.GetRegion;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetRegion, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                var result = element.GetRegion(positionType);
                Rect rc = new Rect();
                rc.x = result.X.ToString();
                rc.y = result.Y.ToString();
                rc.width = result.Width.ToString();
                rc.height = result.Height.ToString();
                msg.info = JsonHelper.JsonByNewtonJson(rc);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.GetRegion;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_GetRegionError, eComponentType.GetRegion, e);
                return GetEncodingStr(rpaException);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string IsElementExist(ISelectorContext selectorData, bool findOnece, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;

            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, findOnece, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.IsElementExist;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_ElementIsEnable, eComponentType.IsElementExist, e);
                return GetEncodingStr(rpaException);
            }
            msg.info = element == null ? "0" : "1";
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string IsElementExistEx(ISelectorContext selectorData, bool findOnece, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;

            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, findOnece, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.IsSimilarElementExist;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_ElementIsEnable, eComponentType.IsSimilarElementExist, e);
                return GetEncodingStr(rpaException);
            }
            msg.info = element == null ? "0" : "1";
            if (element != null)
            {
                Rectangle rec = element.GetRegion(3);

                msg.info += string.Format("({0}:{1}:{2}:{3})", rec.X, rec.Y, rec.Width, rec.Height);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string GetSubElements(ISelectorContext selectorData, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;

            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.GetSubElements;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetSubElements, e);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                {
                    var result = element.GetChild(selectorData.Selector);
                    msg.info = result;
                }
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.GetSubElements;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_GetSubElementError, eComponentType.GetSubElements, e);
                return GetEncodingStr(rpaException);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        public string ElementShot(ISelectorContext selectorData, string filepath, int timeout = 10000)
        {

            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;

            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selectorData.Selector, version: selectorData.Version);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.ElementShot;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.ElementShot, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                element.CaptureInformativeScreenshotToFile(filepath);
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.ElementShot;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_ScreenshotError, eComponentType.ElementShot, e);
                return GetEncodingStr(rpaException);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public float GetScaler()
        {
            System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(new IntPtr(1970662));
            IntPtr desktop = g.GetHdc();
            int LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);
            float ScreenScalingFactor = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;
            return ScreenScalingFactor; // 1.25 = 125%
        }

        public string SetText(string selector, string txt, int isReplace = 0, int inputType = 1, int interval = 0, int isVerify = 0, int timeout = 10000)
        {
            JMessage msg = new JMessage();
            logger.LogDebug($"Start SetText");
            JavaNode element = null;
            try
            {
                element = JavaNode.GetElementFromSelector(selector);
            }
            catch (RPAException e)
            {
                LoggingExtensions.LogError(logger, e, $"SetText RPAEx");
                e.ComponentType = eComponentType.SetText;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception ex)
            {
                LoggingExtensions.LogError(logger, ex, $"SetText xEx");
                var rpaException = new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.SetText, ex);
                return GetEncodingStr(rpaException);
            }
            try
            {
                var replace = isReplace == 0 ? true : false;
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                if (inputType == 0)
                {
                    var isSimulate = inputType == 1 ? true : false;
                    element.SetText(txt, isReplace, isSimulate);
                }
                else if (inputType == 1)
                {
                    //bool bExit = false;
                    Thread excThread = new Thread(new System.Threading.ThreadStart(() =>
                    {
                        try
                        {
                            element.MouseClick(0, 0, true, 0);

                            SetTextEx(replace, interval, txt);
                        }
                        catch (Exception ex)
                        {
                            LoggingExtensions.LogError(logger, ex, $"JavaHostExecute Err");
                        }
                        finally
                        {
                            //bExit = true;
                        }
                    }
                 ));
                    excThread.SetApartmentState(ApartmentState.STA);
                    excThread.Start();
                    excThread.Join(2 * 60 * 1000);//两分钟超时时间
                    logger.LogError($"JavaHostExecute Simulate join end");
                    //while (bExit == false)
                    //{
                    //    Thread.Sleep(500);
                    //}
                }
                //赋值粘贴的方式文本输入
                else if (inputType == 2)
                {
                    //bool bExit = false;
                    Thread excThread = new Thread(new System.Threading.ThreadStart(() =>
                    {
                        try
                        {
                            string value = element.GetText();

                            element.MouseClick(0, 0, true, 0);
                            ClipboardPaste(txt, isReplace, "");
                        }
                        catch (Exception ex)
                        {
                            LoggingExtensions.LogError(logger, ex, "JavaHostExecute Err");
                        }
                        finally
                        {
                            //bExit = true;
                        }
                    }
                        ));
                    excThread.SetApartmentState(ApartmentState.STA);
                    excThread.Start();
                    excThread.Join(5000);
                    logger.LogError($"JavaHostExecute paste join end");
                    //while (bExit == false)
                    //{
                    //    Thread.Sleep(500);
                    //}
                }

                var verify = false;

                if (isVerify == 1)
                {

                    if (replace)
                        verify = element.GetText().Equals(txt);
                    else
                        verify = element.GetText().EndsWith(txt);
                }
                try
                {
                    msg.info = "True";
                    if (replace && isVerify == 1 && !verify && inputType == 1)
                    {
                        msg.info = "False";
                        element.SetText(txt, isReplace, false);
                    }
                }
                catch (Exception)
                {
                }


            }
            catch (RPAException e)
            {
                LoggingExtensions.LogError(logger, e, $"SetText RPAEx2");
                e.ComponentType = eComponentType.SetText;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                LoggingExtensions.LogError(logger, e, "SetText xEx2");
                var rpaException = new RPAException(eErrorMessageType.Error_SetTextError, eComponentType.SetText, e);
                return GetEncodingStr(rpaException);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }
        private JMessage SetState(string param)
        {
            JMessage msg = new JMessage();
            try
            {
                var hwnd = NativeMethods.GetForegroundWindow();
                var hIme = NativeMethods.ImmGetDefaultIMEWnd(hwnd);
                if (param.Equals("zh_cn"))
                {
                    NativeMethods.SendMessage(hIme, 0x283, new IntPtr(2), new IntPtr(1));
                    NativeMethods.SendMessage(hIme, 0x283, new IntPtr(6), new IntPtr(1));
                }
                else
                {
                    NativeMethods.SendMessage(hIme, 0x283, new IntPtr(2), new IntPtr(0));
                    NativeMethods.SendMessage(hIme, 0x283, new IntPtr(6), new IntPtr(0));
                }

            }
            catch (Exception)
            {
                msg.code = -1;
                //msg.UnifyCode = "-1";
                msg.message = "设置输入法状态失败";

            }
            return msg;
        }
        private JMessage GetState()
        {
            JMessage msg = new JMessage();
            try
            {
                var hwnd = NativeMethods.GetForegroundWindow();
                var hIme = NativeMethods.ImmGetDefaultIMEWnd(hwnd);

                var open_zn = NativeMethods.SendMessage(hIme, 0x283, new IntPtr(5), IntPtr.Zero);
                if (open_zn > 0)
                {
                    var mode = NativeMethods.SendMessage(hIme, 0x283, new IntPtr(1), IntPtr.Zero);
                    var state = mode & 3;
                    int _char = 0;
                    if ((state & 0x400) > 0) _char = 3;//中文标点
                    else if ((state & 8) > 0) _char = 2;//英文全角
                    else _char = 1;//英文半角
                    var _state = state > 0 ? "中文" : "英文";
                    msg.info = new JObject() { { "state", _state } }.ToString();
                }
                else
                {
                    msg.info = new JObject() { { "state", "英文" } }.ToString();
                }
            }
            catch (Exception)
            {
                msg.code = -1;
                //msg.UnifyCode = "-1";
                msg.message = "获取输入法状态失败";

            }
            return msg;
        }
        #region 新方式处理模拟输入
        public void SetTextEx(bool replace, int interval, string text)
        {
            if (replace)
            {
                AutoItHelper.ClearWords(interval);
            }
            else
            {
                AutoItHelper.MoveCursorToEnd(interval);
            }

            JMessage result = GetState();
            var info = result.info;
            var state = JObject.Parse(info.ToString());
            bool isChange = false;
            if (state.Value<string>("state").Equals("中文"))
            {
                SetState("en_us");
            }

            AutoItHelper.Send(text, false, sendKeyDelay: interval);
        }

        #endregion
        /// <summary>
        /// 通过剪切板的方式实现文本输入
        /// </summary>
        /// <param name="text">输入的值</param>
        /// <param name="isReplace">是否覆盖</param>
        /// <param name="originalValue">原有值</param>
        public static void ClipboardPaste(string text, int isReplace, string originalValue = "")
        {
            try
            {
                var replace = isReplace == 0 ? true : false;
                //if (!string.IsNullOrEmpty(originalValue))
                //{
                //    //logger.LogDebug("按键清空输入框");
                //    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                //    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                //    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);

                //    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
                //    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                //    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                //    Thread.Sleep(200);
                //}
                if (replace)
                {
                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);

                    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
                    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                    Thread.Sleep(200);
                }
                else
                {
                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);

                    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                    Thread.Sleep(200);

                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.RIGHT);
                    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.RIGHT);
                    Thread.Sleep(200);
                }

                //text = replace ? text : originalValue + text;
                //System.Windows.Clipboard.SetText(text);
                SetClipBoard(text);
                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_V);

                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_V);
                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                Thread.Sleep(200);
                //Clipboard.Clear();
                ClearClipBoard();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }

        public static void SetClipBoard(string text)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                logger.LogError($"Java 设置剪切板异常 {ex.ToString()}");
            }
        }

        public static void ClearClipBoard()
        {
            try
            {
                System.Windows.Clipboard.Clear();
            }
            catch (Exception ex)
            {
                logger.LogError($"Java 清空剪切板异常 {ex.ToString()}");
            }
        }

        public enum DeviceCap
        {
            /// <summary>
            /// Device driver version
            /// </summary>
            DRIVERVERSION = 0,
            /// <summary>
            /// Device classification
            /// </summary>
            TECHNOLOGY = 2,
            /// <summary>
            /// Horizontal size in millimeters
            /// </summary>
            HORZSIZE = 4,
            /// <summary>
            /// Vertical size in millimeters
            /// </summary>
            VERTSIZE = 6,
            /// <summary>
            /// Horizontal width in pixels
            /// </summary>
            HORZRES = 8,
            /// <summary>
            /// Vertical height in pixels
            /// </summary>
            VERTRES = 10,
            /// <summary>
            /// Number of bits per pixel
            /// </summary>
            BITSPIXEL = 12,
            /// <summary>
            /// Number of planes
            /// </summary>
            PLANES = 14,
            /// <summary>
            /// Number of brushes the device has
            /// </summary>
            NUMBRUSHES = 16,
            /// <summary>
            /// Number of pens the device has
            /// </summary>
            NUMPENS = 18,
            /// <summary>
            /// Number of markers the device has
            /// </summary>
            NUMMARKERS = 20,
            /// <summary>
            /// Number of fonts the device has
            /// </summary>
            NUMFONTS = 22,
            /// <summary>
            /// Number of colors the device supports
            /// </summary>
            NUMCOLORS = 24,
            /// <summary>
            /// Size required for device descriptor
            /// </summary>
            PDEVICESIZE = 26,
            /// <summary>
            /// Curve capabilities
            /// </summary>
            CURVECAPS = 28,
            /// <summary>
            /// Line capabilities
            /// </summary>
            LINECAPS = 30,
            /// <summary>
            /// Polygonal capabilities
            /// </summary>
            POLYGONALCAPS = 32,
            /// <summary>
            /// Text capabilities
            /// </summary>
            TEXTCAPS = 34,
            /// <summary>
            /// Clipping capabilities
            /// </summary>
            CLIPCAPS = 36,
            /// <summary>
            /// Bitblt capabilities
            /// </summary>
            RASTERCAPS = 38,
            /// <summary>
            /// Length of the X leg
            /// </summary>
            ASPECTX = 40,
            /// <summary>
            /// Length of the Y leg
            /// </summary>
            ASPECTY = 42,
            /// <summary>
            /// Length of the hypotenuse
            /// </summary>
            ASPECTXY = 44,
            /// <summary>
            /// Shading and Blending caps
            /// </summary>
            SHADEBLENDCAPS = 45,

            /// <summary>
            /// Logical pixels inch in X
            /// </summary>
            LOGPIXELSX = 88,
            /// <summary>
            /// Logical pixels inch in Y
            /// </summary>
            LOGPIXELSY = 90,

            /// <summary>
            /// Number of entries in physical palette
            /// </summary>
            SIZEPALETTE = 104,
            /// <summary>
            /// Number of reserved entries in palette
            /// </summary>
            NUMRESERVED = 106,
            /// <summary>
            /// Actual color resolution
            /// </summary>
            COLORRES = 108,

            // Printing related DeviceCaps. These replace the appropriate Escapes
            /// <summary>
            /// Physical Width in device units
            /// </summary>
            PHYSICALWIDTH = 110,
            /// <summary>
            /// Physical Height in device units
            /// </summary>
            PHYSICALHEIGHT = 111,
            /// <summary>
            /// Physical Printable Area x margin
            /// </summary>
            PHYSICALOFFSETX = 112,
            /// <summary>
            /// Physical Printable Area y margin
            /// </summary>
            PHYSICALOFFSETY = 113,
            /// <summary>
            /// Scaling factor x
            /// </summary>
            SCALINGFACTORX = 114,
            /// <summary>
            /// Scaling factor y
            /// </summary>
            SCALINGFACTORY = 115,

            /// <summary>
            /// Current vertical refresh rate of the display device (for displays only) in Hz
            /// </summary>
            VREFRESH = 116,
            /// <summary>
            /// Vertical height of entire desktop in pixels
            /// </summary>
            DESKTOPVERTRES = 117,
            /// <summary>
            /// Horizontal width of entire desktop in pixels
            /// </summary>
            DESKTOPHORZRES = 118,
            /// <summary>
            /// Preferred blt alignment
            /// </summary>
            BLTALIGNMENT = 119
        }
        public enum CLICKTYPE
        {
            LEFT_CLICK,
            LEFT_DOUBLECLICK,
            RIGHT_CLICK,
            RIGHT_DOUBLECLICK
        }
    }
}
