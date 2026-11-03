using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.OcrOffline.Utils
{
    /// <summary>
    /// Saas鉴权请求
    /// </summary>
    #region

    public class SaasAuthenticationHelper
    {
        private static ILogger logger = Logger.OfContext();

        /// <summary>
        /// 请求saas平台上注册的服务
        /// </summary>
        /// <param name="_Token"></param>
        /// <param name="_SaasParam"></param>
        /// <returns></returns>
        #region Http post请求
        public static bool RequestSaasService(CommonHttpParam _CommonHttpParam, SaasParam _SaasParam, ref string _ResponseValue, ref string _ErrorMessage)
        {
            bool bRet = false;
            logger.LogInformation($"Url:{_CommonHttpParam.Url} Token:{_SaasParam.Token}");
            if (string.IsNullOrEmpty(_CommonHttpParam.Url) || string.IsNullOrEmpty(_SaasParam.Token))
            {
                string _Flag1 = string.IsNullOrEmpty(_CommonHttpParam.Url) ? "URL" : " ";
                string _Flag2 = string.IsNullOrEmpty(_SaasParam.Token) ? "Token" : " ";
                _ErrorMessage = Localize($"缺少{_Flag1}{_Flag2}", "SaasAuthenticationHelper_A11920F9");
                return bRet;
            }

            try
            {
                #region Post请求
                HttpWebRequest _HttpRequest = null;
                if (true == _CommonHttpParam.Url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("进入Https 逻辑");
                    //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | (SecurityProtocolType)3072;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);
                    ServicePointManager.Expect100Continue = true;
                    _HttpRequest = (HttpWebRequest)WebRequest.Create(_CommonHttpParam.Url);
                    //_HttpRequest.ProtocolVersion = HttpVersion.Version10;
                    logger.LogInformation("Https 逻辑");
                }
                else
                {
                    logger.LogInformation("进入Http 逻辑");
                    _HttpRequest = (HttpWebRequest)WebRequest.Create(_CommonHttpParam.Url);
                    logger.LogInformation("Http 逻辑");
                }
                //HttpWebRequest对象用来发起请求

                //设置Header参数
                if (_CommonHttpParam.Header != null && _CommonHttpParam.Header.Count > 0)
                {
                    foreach (string key in _CommonHttpParam.Header.AllKeys)
                    {
                        _HttpRequest.Headers.Add(key, _CommonHttpParam.Header[key]);
                    }
                }

                //_HttpRequest.Headers.Add("Content-Type", _CommonHttpParam.Content_Type); //会报错 不能直接添加到header里
                _HttpRequest.Headers.Add("Authorization", _SaasParam.Token);

                _HttpRequest.Method = _CommonHttpParam.Method;
                _HttpRequest.ContentType = _CommonHttpParam.Content_Type;
                _HttpRequest.Timeout = _CommonHttpParam.TimeOut;

                Dictionary<string, object> ParamCollection = new Dictionary<string, object>();
                ParamCollection.Add("method", _SaasParam.MethodName);
                ParamCollection.Add("version", _SaasParam.Version);
                ParamCollection.Add("charset", "utf-8");
                ParamCollection.Add("format", "json");
                ParamCollection.Add("timestamp", _SaasParam.Timestamp);
                foreach (var pair in _CommonHttpParam.ParamCollection)
                {
                    ParamCollection.Add(pair.Key, pair.Value);
                }

                string PostData = JsonConvert.SerializeObject(ParamCollection);

                if (!string.IsNullOrEmpty(PostData))
                {
                    var bytes = Encoding.UTF8.GetBytes(PostData);

                    _HttpRequest.ContentLength = bytes.Length;

                    using (var writeStream = _HttpRequest.GetRequestStream())
                    {
                        writeStream.Write(bytes, 0, bytes.Length);
                        logger.LogInformation("发送请求");
                    }
                }
                #endregion

                #region 获取返回
                using (var response = (HttpWebResponse)_HttpRequest.GetResponse())
                {
                    var responseValue = string.Empty;

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        var message = string.Format("请求数据失败. 返回的 HTTP 状态码：{0}", response.StatusCode);
                        throw new ApplicationException(message);
                    }

                    using (var responseStream = response.GetResponseStream())
                    {
                        if (responseStream != null)
                            using (var reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")))
                            {
                                responseValue = reader.ReadToEnd();
                                _ResponseValue = responseValue;
                                bRet = true;
                            }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _ErrorMessage = ex.ToString();
                logger.LogInformation($"请求异常：{ex.ToString()}");
                ///日志
            }

            return bRet;
        }
        #endregion


        /// <summary>
        /// 证书验证 无实际代码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        { // Always accept
          //Console.WriteLine("accept " + certificate.GetName());
            return true; //总是接受
        }

    }
    #endregion

    /// <summary>
    /// Saas鉴权部分参数
    /// </summary>
    #region 鉴权类
    public class SaasParam
    {
        /// <summary>
        /// 服务名
        /// </summary>
        public string MethodName = string.Empty;
        public long Timestamp = GetTimeStamp();
        public string Version = string.Empty;
        //public string Image = "image_base64";
        public string Token = string.Empty;
        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        /// <returns></returns>
        public static long GetTimeStamp()
        {
            //此方法可用
            DateTime startTime = TimeZoneInfo.ConvertTime(new System.DateTime(1970, 1, 1), TimeZoneInfo.Utc, TimeZoneInfo.Local); //当地时区
            long timeStamp = (long)(DateTime.Now - startTime).TotalMilliseconds; //相差毫秒数
            return timeStamp;
        }

        public SaasParam(string _MethodName, string _Version, string _Token)
        {
            MethodName = _MethodName;
            Version = _Version;
            Token = _Token;
        }
    }
    #endregion

    /// <summary>
    /// 通用http参数
    /// </summary>
    #region Http 入参
    public class CommonHttpParam
    {
        public string Url = InternetStateHelper.SaasPlatformUrl;
        public int TimeOut = 100000;

        public Dictionary<string, object> ParamCollection = new Dictionary<string, object>();
        /// <summary>
        /// Post/Get
        /// </summary>
        public string Method = "Post";
        public string Content_Type = "application/json";//"application/json";

        private WebHeaderCollection header = new WebHeaderCollection();
        /// <summary>
        /// header对象
        /// </summary>
        public WebHeaderCollection Header
        {
            get { return header; }
            set { header = value; }
        }
    }
    #endregion
}
