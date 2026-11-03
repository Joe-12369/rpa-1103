using II.RPA.Core.Runtime;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction
{
    public abstract class BaseActionProvider : IClientActionProvider
    {
        protected const string SuccessCode = "0";
        protected const string FailCode = "1";
        protected const string CaptureFailMessage = "拾取失败";
        protected readonly ILogger m_Logger;
        /// <summary>
        /// 传给plugin的是绝对路径
        /// </summary>
        protected string m_APicturePath;
        /// <summary>
        /// 返回前端的是相对路径
        /// </summary>
        protected string m_RPicturePath;
        /// <summary>
        /// 图片随即名
        /// </summary>
        protected string m_PictureRandomName;
        /// <summary>
        /// 流程包路径
        /// </summary>
        protected string m_PackagePath;
        #region Z-Brain相关、变量名同Engine：config.py
        /// <summary>
        /// 执行
        /// </summary>
        protected string cvExecutionServiceURL = "http://127.0.0.1:6440/cv_execution_service/";
        /// <summary>
        /// 文本提取
        /// </summary>
        protected string cvserver = "http://127.0.0.1:6440/get_text_content_service";
        /// <summary>
        /// 拾取
        /// </summary>
        protected string cvPickServer = "http://127.0.0.1:6440/cv_element_service_setup_detect/";
        /// <summary>
        /// Z-Brain http服务端口
        /// </summary>
        protected int cvPort = 6440;
        /// <summary>
        /// 5.9.5的ui_selector_cv.py中cvserver使用的是saas的url
        /// </summary>
        protected string cv_saas_server = "http://cv-element-service.ext.ii-ai.tech/cv_element_service/";
        /// <summary>
        /// zbrain手机元素拾取接口
        /// </summary>
        protected string cvPhonePickServer = "http://127.0.0.1:6440/cv_element_pickup_phone";
        #endregion

        protected IRuntimeHostController m_RuntimeHostController = null;
        public BaseActionProvider(ILogger aLogger)
        {
            m_Logger = aLogger;
            if (int.TryParse(ConfigurationManager.AppSettings["cvport"], out int port))
            {
                m_Logger.LogDebug($"cvport from config is {port}");
                cvPort = port;
            }
            if (int.TryParse(ConfigurationManager.AppSettings["ActionDefaultTimeoutSecond"], out int anActionDefaultTimeoutSecond))
            {
                m_Logger.LogDebug($"Base TimeoutSeconds from config is {anActionDefaultTimeoutSecond}");
                TimeoutSeconds = anActionDefaultTimeoutSecond;
            }
            ReadConfig();
            m_PictureRandomName = $"{new Random().Next(1, 99999)}-{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}.png".Trim().Replace(" ", string.Empty);
            m_RPicturePath = Path.Combine("res", m_PictureRandomName);
        }
        public BaseActionProvider(ILogger aLogger, IRuntimeHostController aRuntimeHostController) : this(aLogger)
        {
            m_RuntimeHostController = aRuntimeHostController;
        }
        public virtual string ActionName
        {
            get;
        }
        /// <summary>
        /// 流程包路径
        /// </summary>
        public string PackagePath
        {
            get
            {
                return m_PackagePath;
            }
            set
            {
                m_PackagePath = value;
                m_APicturePath = Path.Combine(m_PackagePath, "res", m_PictureRandomName);
            }
        }
        /// <summary>
        /// 是都需要STA线程、目前都放到主线程执行、此字段暂时没用
        /// </summary>
        public virtual bool NeedSTA
        {
            get;
            set;
        } = false;
        /// <summary>
        /// action执行超时时间
        /// </summary>
        public virtual int TimeoutSeconds
        {
            get;
            private set;
        } = 600;

        private void ReadConfig()
        {
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["cvExecutionServiceURL"]))
            {
                cvExecutionServiceURL = ConfigurationManager.AppSettings["cvExecutionServiceURL"];
            }
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["cvserver"]))
            {
                cvserver = ConfigurationManager.AppSettings["cvserver"];
            }
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["cvPickServer"]))
            {
                cvPickServer = ConfigurationManager.AppSettings["cvPickServer"];
            }
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["cv_saas_server"]))
            {
                cv_saas_server = ConfigurationManager.AppSettings["cv_saas_server"];
            }
        }

        #region action解析方法:调用顺序ResolveAction()->ResolveReqVal()->PreResolveRetVal()->ResolveRetVal()
        /// <summary>
        /// 解析Action->pluginName、pluginAction
        /// </summary>
        /// <param name="aReqVal"></param>
        /// <returns></returns>
        public abstract PluginRunAction ResolveAction(string aReqVal);
        /// <summary>
        /// 请求参数解析
        /// </summary>
        /// <param name="aReqVal"></param>
        /// <returns></returns>
        public virtual string ResolveReqVal(string aReqVal)
        {
            return aReqVal;
        }
        /// <summary>
        /// 预处理：对于个别请求可以检测入参的合法性,若入参不合法直接返回结果
        /// </summary>
        /// <param name="aReqVal"></param>
        /// <param name="aReqValOrigin"></param>
        /// <returns></returns>
        public virtual PluginRunResult PreResolveRetVal(string aReqVal, string aReqValOrigin = null)
        {
            return new PluginRunResult
            {
                Code = "0",
                Message = "go on",
                Result = ""
            };
        }
        /// <summary>
        /// 返回值解析
        /// </summary>
        /// <param name="aRetValFromPlugin"></param>
        /// <returns></returns>
        public virtual PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            if (string.IsNullOrEmpty(aRetValFromPlugin))
            {
                return new PluginRunResult
                {
                    Code = "99",
                    Message = "internal error",
                };
            }
            try
            {
                var retObject = JObject.Parse(aRetValFromPlugin);
                if (retObject == null)
                {
                    m_Logger.LogError(aRetValFromPlugin);
                    return new PluginRunResult
                    {
                        Code = "1",
                        Message = "返回值解析失败，请重试。若仍未解决，请联系运营人员。",
                        Result = "",
                    };
                }
                m_Logger.LogDebug(aRetValFromPlugin);
                return new PluginRunResult
                {
                    Code = retObject.GetValue("code") == null ? "1" : retObject.GetValue("code").ToString(),
                    Message = retObject.GetValue("message") == null ? "返回值解析失败，请重试。若仍未解决，请联系运营人员。" : retObject.GetValue("message").ToString(),
                    Result = retObject.GetValue("info")?.ToString(),
                    Extend = retObject.GetValue("extend")?.ToString()
                };
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                m_Logger.LogError($"Fail to parse json:{aRetValFromPlugin}");
                return new PluginRunResult
                {
                    Code = "1",
                    Message = $"返回值解析失败，请重试。若仍未解决，请联系运营人员。",
                    Result = "{}",
                    Extend = ""
                };
            }
        }
        /// <summary>
        /// 返回值解析-泛型方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="P"></typeparam>
        /// <param name="aRetValFromPlugin"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual T ResolveRetVal<T, P>(P aRetValFromPlugin)
        {
            return default;
        }

        public virtual string GetRPicturePath(object picPath)
        {
            var f = picPath.ToString();
            return f.Substring(f.LastIndexOf(@"res\"));
        }
        #endregion
    }
}
