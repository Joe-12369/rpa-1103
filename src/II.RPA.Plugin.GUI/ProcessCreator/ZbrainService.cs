using FlaUI.Core.Logging;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator
{
    public class ZbrainService
    {
        private static readonly Microsoft.Extensions.Logging.ILogger logger = Logging.Logger.OfContext();
        private readonly HttpClient HttpClientInstance;
        private readonly string m_Url;
        public ZbrainService(string anUrl)
        {
            logger.LogInformation($"DefaultConnectionLimit:{ServicePointManager.DefaultConnectionLimit}");
            ServicePointManager.DefaultConnectionLimit = 128;
            logger.LogInformation($"CurrentConnectionLimit:{ServicePointManager.DefaultConnectionLimit}");
            if (string.IsNullOrEmpty(anUrl))
            {
                m_Url = "http://127.0.0.1:6440";
            }
            else
            {
                m_Url = anUrl;
            }
            HttpClientInstance = new HttpClient();
            HttpClientInstance.DefaultRequestHeaders.Add("CLIENT-USER", GlobleConfigurations.ClientUser);
            HttpClientInstance.DefaultRequestHeaders.Add("CLIENT-MAC", GlobleConfigurations.ClientMac);
            HttpClientInstance.DefaultRequestHeaders.Add("engineCode", GlobleConfigurations.EngineCode);
            HttpClientInstance.DefaultRequestHeaders.Add("Accept", "*/*");
            HttpClientInstance.Timeout = TimeSpan.FromMilliseconds(1000);
        }
        public ZbrainService(string anUrl, TimeSpan timeout) : this(anUrl)
        {
            HttpClientInstance.Timeout = timeout;
        }
        public string ZBrainEndPoint
        {
            get 
            {
                if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
                {
                    return m_Url;
                }
                else
                {
                    return GlobleConfigurations.ZBrainServicePath;
                }
            }
        }
        public async Task<string> SaveUserCustomModel()
        {
            try
            {
                logger.LogDebug($"SaveUserCustomModel start");
                var url = SomeUtils.SignHttpRequestToZBrain($"{ZBrainEndPoint}/component/recommender/save");
                using (var response = await HttpClientInstance.GetAsync(url))
                {
                    using (var responseEnsure = response.EnsureSuccessStatusCode())
                    {
                        var responseString = await responseEnsure.Content.ReadAsStringAsync();
                        logger.LogDebug($"SaveUserCustomModel end:{responseString}");
                        return responseString;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"SaveUserCustomModel error: {ZBrainEndPoint}");
                return null;
            }
        }
        /// <summary>
        /// 加载模型
        /// </summary>
        /// <returns></returns>
        public async Task<string> LoadRecommendModel()
        {
            try
            {
                logger.LogDebug($"LoadRecommendModel start");
                var url = SomeUtils.SignHttpRequestToZBrain($"{ZBrainEndPoint}/component/recommender/load");
                using (var response = await HttpClientInstance.GetAsync(url))
                {
                    using (var responseEnsure = response.EnsureSuccessStatusCode())
                    {
                        var responseString = await responseEnsure.Content.ReadAsStringAsync();
                        logger.LogDebug($"LoadRecommendModel end:{responseString}");
                        return responseString;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"LoadRecommendModel error: {ZBrainEndPoint}");
                return null;
            }
        }
        /// <summary>
        /// 负反馈
        /// </summary>
        /// <param name="aRequestJson"></param>
        /// <returns></returns>
        public async Task<string> DissFeedBack(string aRequestJson)
        {
            try
            {
                logger.LogDebug($"DissFeedBack start:{aRequestJson}");
                HttpContent content = new StringContent(aRequestJson, Encoding.UTF8);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var url = SomeUtils.SignHttpRequestToZBrain($"{ZBrainEndPoint}/component/recommender/forget");
                using (var response = await HttpClientInstance.PostAsync(url, content))
                {
                    using (var responseEnsure = response.EnsureSuccessStatusCode())
                    {
                        var responseString = await responseEnsure.Content.ReadAsStringAsync();
                        logger.LogDebug($"DissFeedBack end:{responseString}");
                        return responseString;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"DissFeedBack error:{ZBrainEndPoint}");
                return null;
            }
        }
        /// <summary>
        /// ai 推荐
        /// </summary>
        /// <param name="aRequestJson"></param>
        /// <returns></returns>
        public async Task<string> AIRecommend(string aRequestJson)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                logger.LogDebug($"AIRecommend start:{aRequestJson}");
                SomeUtils.TraceThreadPool();
                HttpContent content = new StringContent(aRequestJson, Encoding.UTF8);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var url = SomeUtils.SignHttpRequestToZBrain($"{ZBrainEndPoint}/component/recommender/infer");
                using (var response = await HttpClientInstance.PostAsync(url, content))
                {
                    using (var responseEnsure = response.EnsureSuccessStatusCode())
                    {
                        var responseString = await responseEnsure.Content.ReadAsStringAsync();
                        logger.LogDebug($"AIRecommend end-{stopwatch.ElapsedMilliseconds}ms:{responseString}");
                        stopwatch.Stop();
                        return responseString;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"AIRecommend error:{ZBrainEndPoint}");
                return null;
            }
            finally 
            { 
                stopwatch.Stop(); 
            }
        }

        /// <summary>
        /// 元素分类
        /// </summary>
        /// <param name="aRequestJson"></param>
        /// <returns></returns>
        public async Task<string> ElementClass(string aRequestJson)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                logger.LogDebug($"ElementClass start:{aRequestJson}");
                SomeUtils.TraceThreadPool();
                HttpContent content = new StringContent(aRequestJson, Encoding.UTF8);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var url = SomeUtils.SignHttpRequestToZBrain($"{ZBrainEndPoint}/element_class");
                using (var response = await HttpClientInstance.PostAsync(url, content))
                {
                    using (var responseEnsure = response.EnsureSuccessStatusCode())
                    {
                        var responseString = await responseEnsure.Content.ReadAsStringAsync();
                        logger.LogDebug($"ElementClass end-{stopwatch.ElapsedMilliseconds}ms:{responseString}");
                        return responseString;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"ElementClass error:{ZBrainEndPoint}");
                return null;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// 在线学习
        /// </summary>
        /// <param name="aRequestJson"></param>
        /// <returns></returns>
        public async Task<string> LearnOnline(string aComponentId)
        {
            try
            {
                logger.LogDebug($"LearnOnline start:{aComponentId}");
                var url = SomeUtils.SignHttpRequestToZBrain($"{ZBrainEndPoint}/component/recommender/learn?this={aComponentId}");
                using (var response = await HttpClientInstance.GetAsync(url))
                {
                    using (var responseEnsure = response.EnsureSuccessStatusCode())
                    {
                        var responseString = await responseEnsure.Content.ReadAsStringAsync();
                        logger.LogDebug($"LearnOnline end:{responseString}");
                        return responseString;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"LearnOnline error:{ZBrainEndPoint}");
                return null;
            }
        }

        /// <summary>
        /// 获取登录区域
        /// </summary>
        /// <param name="aRequestJson"></param>
        /// <returns></returns>
        public async Task<string> GetLoginComponentData(string aRequestJson)
        {
            try
            {
                logger.LogDebug($"GetLoginComponentData start:{aRequestJson}");
                HttpContent content = new StringContent(aRequestJson, Encoding.UTF8);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var url = SomeUtils.SignHttpRequestToZBrain($"{ZBrainEndPoint}/element_login_det");
                using (var response = await HttpClientInstance.PostAsync(url, content))
                {
                    using (var responseEnsure = response.EnsureSuccessStatusCode())
                    {
                        var responseString = await responseEnsure.Content.ReadAsStringAsync();
                        logger.LogDebug($"GetLoginComponentData end:{responseString}");
                        return responseString;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"GetLoginComponentData error: {ZBrainEndPoint}");
                return null;
            }
        }

        public async Task<string> CheckImageAsync(string aRequestJson)
        {
            try
            {
                logger.LogDebug($"CheckImage start:{aRequestJson}");
                HttpContent content = new StringContent(aRequestJson, Encoding.UTF8);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var url = SomeUtils.SignHttpRequestToZBrain($"{ZBrainEndPoint}/RelativePosition");
                using (var response = await HttpClientInstance.PostAsync(url, content))
                {
                    using (var responseEnsure = response.EnsureSuccessStatusCode())
                    {
                        var responseString = await responseEnsure.Content.ReadAsStringAsync();
                        logger.LogDebug($"CheckImage end:{responseString}");
                        return responseString;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"CheckImage error: {ZBrainEndPoint}");
                return null;
            }
        }

        private void EnsureHttpConnection()
        {
            try
            {
                ThreadPool.GetAvailableThreads(out int workerThreads,
                        out int completionPortThreads);
                logger.LogInformation($"workerThreads:[{workerThreads}]");
                logger.LogInformation($"completionPortThreads:[{completionPortThreads}]");

                //todo
            }
            catch (Exception ex) 
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "CheckThreadPool Error");
            }
        }
    }
}

