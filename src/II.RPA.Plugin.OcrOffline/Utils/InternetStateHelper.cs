using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.Plugin.OcrOffline.Utils
{
    /// <summary>
    /// 网络状态检测
    /// </summary>
    public class InternetStateHelper
    {
        private static ILogger logger = Logger.OfContext();

        //public const string SAASGATEWAY = "sop-gateway.ai-indeed.com";

        //public const string SaasPlatformUrl = "https://sop-gateway.ai-indeed.com";//Saas平台统一访问地址
       
        #region 废弃
        //public const string SaasPlatformUrl = "https://sop-gateway-new.ai-indeed.com";//Saas平台统一访问地址
        //public const string LocalAlgOcrPickUpUrl = "http://127.0.0.1:6440/table/pickup";//本地算法包拾取接口地址
        //public const string LocalAlgOcrAnalyseUrl = "http://127.0.0.1:6440/table/ocr_service";//本地算法包执行接口地址
        #endregion

        public const string SaasPlatformUrl = "https://sop-gateway-new.ai-indeed.com";//Saas平台统一访问地址
                                                                                      
        //本地算法包拾取接口地址
        public static string LocalAlgOcrPickUpUrl
        {
            get { return GetLocalZbrainServicePath(0); }
        }

        //本地算法包执行接口地址
        public static string LocalAlgOcrAnalyseUrl
        {
            get { return GetLocalZbrainServicePath(1); }
        } 

        /// <summary>
        /// 获取当前zbrain服务地址
        /// </summary>
        /// <param name="iServiceType">0，拾取地址；1，解析地址</param>
        /// <returns></returns>
        public static string GetLocalZbrainServicePath(int iServiceType)
        {
            if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
            {
                logger.LogWarning("算法服务地址未同步");
            }

            Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
            if (iServiceType == 0)
            {
                return uri.GetLeftPart(UriPartial.Authority) + "/table/pickup";
            }
            else
            {
                return uri.GetLeftPart(UriPartial.Authority) + "/table/ocr_service";
            }
        }

        /// <summary>
        /// Saas服务是否可用
        /// </summary>
        /// <returns></returns>
        public static bool IsSaasServiceAvailable()
        {
            bool bRet = false;
            try
            {
                if (InternetConnected())//判断是否联公网
                {
                    logger.LogInformation("本机已连网");
                }
                else
                {
                    logger.LogInformation("本机未连网");
                }

                if (IsUrlAvilable(SaasPlatformUrl))
                {
                    logger.LogInformation("目标网址可用");
                    bRet = true;
                }
                else
                {
                    logger.LogInformation("目标网址不可用");
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"判断Saas服务是否可行出现异常");
            }

            return bRet;
        }

        public static bool IsTokenAvailable(string _Token)
        {
            return !string.IsNullOrEmpty(_Token);
        }

        /// <summary>
        /// 判断接口返回信息是否为Token报错
        /// </summary>
        /// <returns></returns>
        public static bool IsResponceTokenErr(string responseString)
        {
            try
            {
                var Object = JsonConvert.DeserializeObject<ResponseTokenErrStruct>(responseString);
                if (Object.code == "20001")
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (responseString.Contains("20001"))
                {
                    return true;
                }
                return false;
            }

            return false;
        }

        /// <summary>
        /// 网址是否可用
        /// </summary>
        /// <returns></returns>
        #region IsUrlAvilable
        public static bool IsUrlAvilable(string Url)
        {
            bool isAvilable = false;
            try
            {
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(Url);
                myHttpWebRequest.Timeout = 1000;
                myHttpWebRequest.Credentials = CredentialCache.DefaultCredentials;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                using (HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse())
                {
                    if (myHttpWebResponse.StatusCode == HttpStatusCode.OK)//如果可以用
                    {
                        isAvilable = true;
                    }
                }
            }
            catch (WebException e)//如果不能用
            {
                isAvilable = false;

                Thread.Sleep(500);
                try
                {
                    HttpWebRequest myHttpWebRequest2 = (HttpWebRequest)WebRequest.Create(Url);
                    myHttpWebRequest2.Timeout = 1000;
                    myHttpWebRequest2.Credentials = CredentialCache.DefaultCredentials;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                    using (HttpWebResponse myHttpWebResponse2 = (HttpWebResponse)myHttpWebRequest2.GetResponse())
                    {
                        if (myHttpWebResponse2.StatusCode == HttpStatusCode.OK)//如果可以用
                        {
                            isAvilable = true;
                        }
                    }
                }
                catch (Exception EX)
                {
                    Console.WriteLine(Localize($"判断网址是否可用 异常:{EX.Message}", "InternetStateHelper_9DAA5717"));
                }
            }
            return isAvilable;
        }
        #endregion

        #region ping网址 弃用
        /// <summary>
        /// Ping命令检测网络是否畅通 弃用
        /// </summary>
        /// <param name="urls">URL数据</param>
        /// <param name="errorCount">ping时连接失败个数</param>
        /// <returns></returns>
        public static bool MyPing(string url)
        {
            bool isconn = false;
            Ping ping = new Ping();
            try
            {
                PingReply pr;
                pr = ping.Send(url);
                if (pr.Status != IPStatus.Success)
                {
                    isconn = false;
                }
                else
                {
                    isconn = true;
                }
                Console.WriteLine("Ping " + url + "    " + pr.Status.ToString());
            }
            catch (Exception ex)
            {
                isconn = false;
                Console.WriteLine($"Saas平台网关访问失败 :{ex.ToString()}");
            }
            return isconn;
        }
        #endregion

        #region 检测当前主机是否连接网络（不等同于能上网）
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int connectionDescription, int reservedValue);

        public static bool InternetConnected()
        {
            int I = 0;
            bool state = InternetGetConnectedState(out I, 0);
            return state;
        }
        #endregion

    }
}

