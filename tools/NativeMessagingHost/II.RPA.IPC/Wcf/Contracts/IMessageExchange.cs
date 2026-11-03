using System.ServiceModel;

namespace II.RPA.IPC
{
    /// <summary>
    /// 默认提供的请求服务协定，主要用于字符串信息交换，通常可以使用Json字符串 在接受端实现具体服务即可。
    /// </summary>
    [ServiceContract]
    [ServiceKnownType(typeof(ResponseResult))]
    [ServiceKnownType(typeof(RequestParams))]
    [ServiceKnownType(typeof(RequestActionParams))]
    public interface IMessageExchange
    {
        [OperationContract]
        void Pending();

        /// <summary>
        /// 使用指定请求参数获取请求数据
        /// </summary>
        /// <param name="request">指定请求参数</param>
        /// <returns>请求数据</returns>
        [OperationContract]
        IResponseResult Request(IRequestParams request);
    }

    public sealed class RequestActionParams : IRequestParams
    {
        /// <summary>
        /// 请求动作
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 请求参数
        /// </summary>
        public string Parameters { get; set; }
    }

    /// <summary>
    /// 请求参数,主要用于服务协定
    /// </summary>
    public sealed class RequestParams : IRequestParams
    {
        /// <summary>
        /// 请求参数
        /// </summary>
        public string Parameters { get; set; }
    }

    /// <summary>
    /// 请求返回结果,主要用于服务协定
    /// </summary>
    public sealed class ResponseResult : IResponseResult
    {
        /// <summary>
        /// 请求返回结果
        /// </summary>
        public string Result { get; set; }
    }
}