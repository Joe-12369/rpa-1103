using Newtonsoft.Json;

namespace II.RPA.IPC
{
    public static class IPCGenericDecoratorExtension
    {
        /// <summary>
        /// 使用指定数据类型进行请求
        /// </summary>
        /// <typeparam name="T">指定数据类型</typeparam>
        /// <typeparam name="M">返回类型</typeparam>
        /// <param name="exchange">交换服务实现接口</param>
        /// <param name="request">指定数据实列</param>
        /// <returns></returns>
        public static M Request<T, M>(this IMessageExchange exchange, T request)
        {
            RequestParams param = new RequestParams();
            param.Parameters = JsonConvert.SerializeObject(request);
            var response = exchange.Request(param);
            M ret = JsonConvert.DeserializeObject<M>(response.Result);
            return ret;
        }

        /// <summary>
        /// 使用指定数据类型进行请求
        /// </summary>
        /// <typeparam name="T">指定数据类型</typeparam>
        /// <typeparam name="M">返回类型</typeparam>
        /// <param name="exchange">交换服务实现接口</param>
        /// <param name="request">指定数据实列</param>
        /// <returns></returns>
        public static M Request<T, M>(this IMessageExchange exchange, string action, T request)
        {
            if (string.IsNullOrEmpty(action))
            {
                return exchange.Request<T, M>(request);
            }
            else
            {
                RequestActionParams param = new RequestActionParams() { Action = action };
                param.Parameters = JsonConvert.SerializeObject(request);
                var response = exchange.Request(param);
                M ret = JsonConvert.DeserializeObject<M>(response.Result);
                return ret;
            }
        }
    }
}