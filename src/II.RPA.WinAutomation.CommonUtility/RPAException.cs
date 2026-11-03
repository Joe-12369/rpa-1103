using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;

namespace II.RPA.WinAutomation.CommonUtility
{
    /// <summary>
    /// 表示在 组件 执行过程中发生的错误。
    /// </summary>
    public class RPAException : Exception
    {
        private static ILogger logger = Logger.OfContext();
        public static string RuntimeVersion { get; set; }

        protected RPAException() : base() { }

        public RPAException(string message) : base(message) { }

        protected RPAException(string message, Exception ex) : base(message, ex) { }

        protected RPAException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        protected ErrorSource ErrorSource { get; set; }
        protected string ErrorSourceCode
        {
            get
            {
                try
                {
                    var code = ((int)this.ErrorSource).ToString().PadLeft(2, '0');
                    return code;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "错误码解析失败");
                }
                return "01";
            }
        }

        protected ErrorApplications ErrorApplications { get; set; }
        protected string ErrorApplicationsCode
        {
            get
            {
                try
                {
                    var code = ((int)this.ErrorApplications).ToString().PadLeft(2, '0');
                    return code;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "错误码解析失败");
                }
                return "04";
            }
        }
        protected ErrorClassify ErrorClassify { get; set; }
        protected string ErrorClassifyCode
        {
            get
            {
                try
                {
                    var code = ((int)this.ErrorClassify).ToString().PadLeft(3, '0');
                    return code;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "错误码解析失败");
                }
                return "001";
            }
        }
        public eGuiType GuiType { get; set; }
        /// 错误码前缀
        /// </summary>
        protected string ErrorCodePrefix { get; set; }

        /// <summary>
        /// 错误码类型
        /// </summary>
        public eErrorMessageType ErrorMessageType { get; set; }

        private string _ErrorCodeSuffixMessage = string.Empty;

        /// <summary>
        /// 错误码
        /// </summary>
        protected string ErrorCodeSuffix
        {
            get
            {
                var code = $"000000{((int)this.ErrorMessageType)}";
                try
                {
                    var value = code.TrimStart('0');
                    if (value.Length < 4)
                        code = code.Substring(code.Length - 3, 3);
                    else
                        code = code.Substring(code.Length - 4, 4);
                    return code;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "错误码解析失败");
                }
                return code;
            }
        }

        /// <summary>
        /// 错误码后缀对应的信息
        /// </summary>
        public string ErrorCodeSuffixMessage
        {
            get
            {
                //优先获取配置文件的错误信息
                if (string.IsNullOrEmpty(_ErrorCodeSuffixMessage))
                {
                    _ErrorCodeSuffixMessage = ErrorMessageConfig.GetMessageByCode(this.ErrorCodeSuffix);
                }
                //配置文件未配置错误信息，尝试获取 错误信息特性中的描述
                if (string.IsNullOrEmpty(_ErrorCodeSuffixMessage))
                {
                    string name = ErrorMessageType.ToString();
                    FieldInfo fieldInfo = ErrorMessageType.GetType().GetField(name);
                    ErrorMessageAttribute errorMessageAttr = fieldInfo.GetCustomAttributes(typeof(ErrorMessageAttribute), false).FirstOrDefault() as ErrorMessageAttribute;
                    if (errorMessageAttr != null)
                    {
                        _ErrorCodeSuffixMessage = errorMessageAttr.Discription;
                    }
                }
                return _ErrorCodeSuffixMessage;
            }
            set
            {
                _ErrorCodeSuffixMessage = value;
            }
        }

        /// <summary>
        /// 错误信息的内容，不含错误码和模块名
        /// </summary>
        public string ErrorMessageContent
        {
            get
            {
                return ErrorCodeSuffixMessage;
            }
        }

        private eComponentType _ComponentType = eComponentType.None;

        /// <summary>
        /// 组件类型
        /// </summary>
        public eComponentType ComponentType
        {
            get
            {
                return _ComponentType;
            }
            set
            {
                _ComponentType = value;
                if (!value.Equals(eComponentType.ExecuteExcel))
                {
                    _ComponentName = string.Empty;
                }
            }
        }

        private string _ComponentName = string.Empty;

        /// <summary>
        /// 组件名称
        /// </summary>
        public string ComponentName
        {
            get
            {
                if (string.IsNullOrEmpty(_ComponentName))
                {
                    string name = ((int)ComponentType).ToString("000");//组件号位数不变
                    _ComponentName = ComponentMessageConfig.GetInstance().GetComponentTextByCode(name);
                    //string name = ComponentType.ToString();
                    //FieldInfo fieldInfo = ComponentType.GetType().GetField(name);
                    //ComponentAttribute componentAttr = fieldInfo.GetCustomAttributes(typeof(ComponentAttribute), false).FirstOrDefault() as ComponentAttribute;
                    //if (componentAttr != null)
                    //{
                    //    _ComponentName = componentAttr.Name;
                    //}
                }
                return _ComponentName;
            }

            set
            {
                _ComponentName = value;
            }
        }

        /// <summary>
        /// 组件码 3位
        /// </summary>
        public string ComponentCode
        {
            get
            {
                var componentCode = $"000000{((int)this.ComponentType)}";
                componentCode = componentCode.Substring(componentCode.Length - 3, 3);
                return componentCode;
            }
        }

        /// <summary>
        /// 最终错误码
        /// </summary>
        public int ErrorCode
        {
            get
            {
                int.TryParse($"{ErrorCodePrefix}{ComponentCode}{ErrorCodeSuffix}", out int code);
                return code;
            }
        }
        public string UnifyErrorCode
        {
            get
            {
                var componentCode = ((int)this.ComponentType).ToString().PadLeft(4, '0');
                var errorCode = ((int)this.ErrorMessageType).ToString().PadLeft(4, '0');///??是否需要改
                return $"{ErrorSourceCode}{ErrorApplicationsCode}{ErrorClassifyCode}{componentCode}{errorCode}";
            }
        }
        /// <summary>
        /// 最终反给engine 的错误提示信息
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                Version v = new Version("6.3.2.0");
                logger.LogInformation($"runtimeHostVersion {version.ToString()},runtimeVersion {RuntimeVersion}");
                if (version < v)
                {
                    logger.LogInformation($"runtimeVersion {version.ToString()} <  6.3.2.0");
                    if (!string.IsNullOrEmpty(this._customMessage))
                    {
                        return $"[{this.ErrorCode}][{this.ComponentName}]{this._customMessage}";
                    }
                    return $"[{this.ErrorCode}][{this.ComponentName}]{this.ErrorCodeSuffixMessage}";
                }
                else
                {
                    int.TryParse(this.ErrorCodePrefix, out int guiType);
                    if (guiType > 400 && guiType < 499)
                    {
                        eGuiType gType = (eGuiType)Enum.ToObject(typeof(eGuiType), guiType);
                        if (!string.IsNullOrEmpty(this._customMessage))
                        {
                            return $"[{this.UnifyErrorCode}][{this.ComponentName}]({gType.ToDescription()}{"类型".I18n("RPAException_1951D753")}){this._customMessage}";
                        }
                        return $"[{this.UnifyErrorCode}][{this.ComponentName}]({gType.ToDescription()}{"类型".I18n("RPAException_1951D753")}){this.ErrorCodeSuffixMessage}";
                    }
                    if (!string.IsNullOrEmpty(this._customMessage))
                    {
                        return $"[{this.UnifyErrorCode}][{this.ComponentName}]{this._customMessage}";
                    }
                    return $"[{this.UnifyErrorCode}][{this.ComponentName}]{this.ErrorCodeSuffixMessage}";
                }

            }
        }

        /// <summary>
        /// 最终反给engine 的错误提示信息
        /// </summary>
        public new string Message
        {
            get
            {
                return this.ErrorMessage;
            }
        }

        private string _customMessage = string.Empty;

        /// <summary>
        /// 自定义的错误消息，前提是在实例RPAException时有提供自定义的错误消息，否侧消息部分为空
        /// </summary>
        public string CustomMessage
        {
            set { _customMessage = value; }
            get
            {
                return $"[{this.ErrorCode}][{this.ComponentName}]{this._customMessage}";
            }
        }
        public RPAException(eGuiType guiType, eErrorMessageType errorMsgType) : this(guiType, errorMsgType, eComponentType.None, null)
        {

        }
        public RPAException(eGuiType guiType, eErrorMessageType errorMsgType, Exception ex) : this(guiType, errorMsgType, eComponentType.None, ex)
        {

        }
        public RPAException(eErrorMessageType errorMsgType) : this(eGuiType.None, errorMsgType, eComponentType.None, null)
        {

        }

        public RPAException(eErrorMessageType errorMsgType, Exception ex) : this(eGuiType.None, errorMsgType, eComponentType.None, ex)
        {

        }

        public RPAException(eErrorMessageType errorMsgType, string customMessage) : this(errorMsgType, eComponentType.None, customMessage)
        {

        }

        public RPAException(eErrorMessageType errorMsgType, string componentType, string customMessage)
        {
            this._customMessage = customMessage;
            this._ComponentName = componentType;
            this.ErrorCodePrefix = ((int)eGuiType.None).ToString();
            this.ErrorMessageType = errorMsgType;
        }

        public RPAException(eErrorMessageType errorMsgType, eComponentType componentType) : this(eGuiType.None, errorMsgType, componentType, null)
        {

        }

        /// <summary>
        /// RPA通用异常类型
        /// </summary>
        /// <param name="errorMsgType"></param>
        /// <param name="componentType"></param>
        /// <param name="customMessage"></param>

        public RPAException(eErrorMessageType errorMsgType, eComponentType componentType, string customMessage) : this(eGuiType.None, errorMsgType, componentType, null)
        {
            _customMessage = customMessage;
        }
        public RPAException(eErrorMessageType errorMsgType, eComponentType componentType, string customMessage, eGuiType _GuiType) : this(_GuiType, errorMsgType, componentType, null)
        {
            _customMessage = customMessage;
        }
        public RPAException(eErrorMessageType errorMsgType, eComponentType componentType, Exception e) : this(eGuiType.None, errorMsgType, componentType, e)
        {

        }

        public RPAException(eGuiType _GuiType, eErrorMessageType errorMsgType, eComponentType componentType) : this(_GuiType, errorMsgType, componentType, null)
        {

        }

        /// <summary>
        /// 表示在 组件 执行过程中发生的错误
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <param name="componentName">组件名</param>
        /// <param name="ex">原生异常信息</param>
        public RPAException(eGuiType _GuiType, int errorCodeSuffix, eComponentType componentType, Exception ex) : this(_GuiType, (eErrorMessageType)errorCodeSuffix, componentType, ex)
        {

        }

        public RPAException(eGuiType _GuiType, eErrorMessageType errorMsgType, eComponentType componentType, Exception ex) : base(ex?.Message, ex)
        {
            this.GuiType = _GuiType;
            this.ErrorCodePrefix = ((int)_GuiType).ToString();
            this.ErrorMessageType = errorMsgType;
            this.ComponentType = componentType;
            //this.ErrorCodeSuffixMessage = ErrorMessageConfig.GetMessageByCode(this.ErrorCodeSuffix);
            //this.ErrorMessage = $"[{this.ErrorCode}][{componentName}]{errorMessage}";

            this.ErrorSource = ErrorSource.Backend;
            this.ErrorApplications = ErrorApplications.Dll;
            this.ErrorClassify = ErrorClassify.Use;

            logger.LogError(ErrorMessage);
            if (ex != null)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, ex.Message);
            }
            else
            {
                var stack = new StackTrace(skipFrames: 1, fNeedFileInfo: true).ToString();
                logger.LogError($"{ErrorMessage}\nStackTrace:\n{stack}");
            }
        }

        public RPAException(eGuiType _GuiType, eErrorMessageType errorMsgType, eComponentType componentType, ErrorSource errorSource, ErrorApplications errorApplications, ErrorClassify errorClassify, Exception ex) : base(ex?.Message, ex)
        {
            this.GuiType = _GuiType;
            this.ErrorCodePrefix = ((int)_GuiType).ToString();
            this.ErrorMessageType = errorMsgType;
            this.ComponentType = componentType;
            //this.ErrorCodeSuffixMessage = ErrorMessageConfig.GetMessageByCode(this.ErrorCodeSuffix);
            //this.ErrorMessage = $"[{this.ErrorCode}][{componentName}]{errorMessage}";
            this.ErrorSource = errorSource;
            this.ErrorApplications = errorApplications;
            this.ErrorClassify = errorClassify;

            logger.LogError(ErrorMessage);

            if (ex != null)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, ex.Message);
            }
        }
    }
}

