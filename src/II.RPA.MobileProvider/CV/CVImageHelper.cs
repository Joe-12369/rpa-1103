using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace II.RPA.MobileProvider.CV
{
    public class CVImageHelper
    {
        private static ILogger logger = Logger.OfContext();
        private static string relativePositionMobile = "/RelativePositionMobile";
        public string InspectImageByCV(ImageMatchingParams prams)
        {
            try
            {
                string zbrainServicePath = GlobleConfig.ZbrainServiceUrl;
                HttpItem item = new HttpItem();
                Uri uri = new Uri(zbrainServicePath);
                item.URL = uri.GetLeftPart(UriPartial.Authority) + relativePositionMobile;
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["source"] = prams.Source;
                dic["template"] = prams.Template;
                dic["thr"] = prams.Thr;
                dic["domain_area"] = new
                {
                    x = prams.DomainArea[0],
                    y = prams.DomainArea[1],
                    width = prams.DomainArea[2],
                    height = prams.DomainArea[3],
                };
                dic["ori_pix"] = prams.OriginalPix;
                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.Html:OK");
                    logger.LogDebug("result.Html:" + (result.Html.Length <= 100 ? result.Html : result.Html.Substring(0, 100)));
                    CVMessage cvmsg = result.Html.ToObject<CVMessage>();
                    if (cvmsg.status == "success")
                    {
                        if (cvmsg.result != null && cvmsg.result.ToString() != "[]")
                        {
                            return cvmsg.result.ToString();
                        }
                        else
                        {
                            throw new Exception(Localize($"算法识别图像失败:{cvmsg.message}", "controlType_CVImageHelper_AB4162DE"));
                        }
                    }
                    else
                    {
                        throw new Exception(cvmsg.message);
                    }
                }
                else
                {
                    logger.LogError($"获取图像坐标信息异常.{result?.Html}");
                    throw new Exception("图像识别异常".I18n("controlType_CVImageHelper_5C540EF7"));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw;
            }
        }

        private HttpResult GetHtml(HttpItem item, Dictionary<string, object> dicParams)
        {
            string saltStr = EncryptUtils.MD5Encrypt("indeedRpaCVOfflineSalt" + GlobleConfigurations.UserToken);
            DateTime DateNow = DateTime.Now;
            long t = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            dicParams.Add("t", t);
            if (!dicParams.ContainsKey("timeout"))
            {
                dicParams.Add("timeout", item.Timeout);
            }
            var dicParams_sort = (from k in dicParams
                                  orderby k.Key
                                  select k).ToDictionary(kv => kv.Key, kv => kv.Value ?? "");
            StringBuilder sb_salt = new StringBuilder();
            List<KeyValuePair<string, object>> sgin_params = new List<KeyValuePair<string, object>>();
            string signSourceStr = "{0}&{1}&{2}";
            Uri uri = new Uri(item.URL);
            //item.Postdata = dicParams_sort.ToJson();
            foreach (var p in dicParams_sort)
            {
                //try
                //{
                sb_salt.Append(saltStr);


                //if (p.Value != null)
                //{
                if (p.Value.GetType().IsArray)
                {
                    continue;
                }
                if (!(p.Value is string) && p.Value.GetType().IsClass)
                {
                    continue;
                }
                //}
                sgin_params.Add(p);
                logger.LogDebug($"参与签名参数：【{p.Key.ToUpper()}】{(p.Value.ToString().Length > 20 ? p.Value.ToString().Substring(0, 20) : p.Value.ToString())}");
                //}
                //finally
                //{
                //    var pVal = new KeyValuePair<string, object>(p.Key, p.Value ?? "");
                //    sgin_params.Add(pVal);
                //    logger.LogDebug($"参与签名参数：【{pVal.Key.ToUpper()}】{(pVal.Value.ToString().Length > 20 ? pVal.Value.ToString().Substring(0, 20) : pVal.Value.ToString())}");
                //}
            }

            logger.LogDebug(sb_salt.ToString());
            string paramsStr = string.Join("&", sgin_params.Select(o => $"{o.Key.ToUpper()}={(o.Value ?? "")}"));
            signSourceStr = string.Format(signSourceStr, uri.AbsolutePath, sb_salt.ToString(), paramsStr);
            string sign = EncryptUtils.MD5Encrypt(signSourceStr);
            dicParams_sort.Remove("t");

            item.Postdata = dicParams_sort.ToJson();
            item.URL = $"{item.URL}{(item.URL.IndexOf("?") > 0 ? "&" : "?")}sign={sign}&t={t}";
            logger.LogDebug($"CV URL{item.URL}");
            return GetHtml(item);
        }
        private HttpResult GetHtml(HttpItem item)
        {
            item.Header.Add("CLIENT-USER", GlobleConfigurations.ClientUser);
            item.Header.Add("CLIENT-MAC", GlobleConfigurations.ClientMac);
            item.Header.Add("engineCode", GlobleConfigurations.EngineCode);
            logger.LogDebug($"engineCode：{GlobleConfigurations.EngineCode}");
            return new HttpHelper().GetHtml(item);
        }
    }
}
