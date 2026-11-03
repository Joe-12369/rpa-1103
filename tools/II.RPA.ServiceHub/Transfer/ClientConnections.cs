using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.ServiceHub
{
    public class ClientConnections
    {
        private static ConcurrentDictionary<string, ConnectionInfo> connectionMap;
        private static ILogger logger;
        static ClientConnections()
        {
            logger = Logger.OfContext();
            connectionMap = new ConcurrentDictionary<string, ConnectionInfo>();
        }

        /// <summary>
        /// 新增连接信息
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static bool AddConnectionInfo(ConnectionInfo ponnectionInfo)
        {
            if (connectionMap.ContainsKey(ponnectionInfo.ConnectionId))
            {
                return false;
            }
            var pInfo = NetStat.GetUsePortProcessInfo(ponnectionInfo.Port).Result;
            ponnectionInfo.SessionId = pInfo == null ? 0 : pInfo.SessionId;
            logger.LogDebug($"session:{pInfo.SessionId},ProcessName:{pInfo.ProcessName},ProcessId:{pInfo.ProcessId}");
            return connectionMap.TryAdd(ponnectionInfo.ConnectionId, ponnectionInfo);
        }
        /// <summary>
        /// 移除连接信息
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static void RemoveConnectionInfo(string connectionId)
        {
            if (connectionMap.ContainsKey(connectionId))
            {
                connectionMap[connectionId].ConnectionState = ConnectionState.Disconnected;
                connectionMap.TryRemove(connectionId, out _);
            }
        }
        /// <summary>
        /// 判断连接信息是否已经存在
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static bool ExistsConnectionInfo(string connectionId)
        {
            return connectionMap.ContainsKey(connectionId);
        }
        /// <summary>
        /// 获取连接信息
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public static ConnectionInfo GetConnectionInfo(string connectionId)
        {
            if (connectionMap.ContainsKey(connectionId))
            {
                return connectionMap[connectionId];
            }
            return null;
        }
        /// <summary>
        /// 获取连接信息
        /// </summary>
        /// <param name="clientType">客户端类型</param>
        /// <param name="clientFlag">客户端标记</param>
        /// <param name="connectionState">连接状态</param>
        /// <returns></returns>
        public static List<ConnectionInfo> GetConnectionInfo(SignalRClientType clientType, string clientFlag, ConnectionState connectionState)
        {
            var clientConns = ClientConnections.GetConnectionInfo(clientType, clientFlag);
            return clientConns.Where(conn => conn.ConnectionState == connectionState).ToList();
        }
        /// <summary>
        /// 获取连接数量
        /// </summary>
        /// <param name="clientType">客户端类型</param>
        /// <param name="clientFlag">客户端标记</param>
        /// <param name="connectionState">连接状态</param>
        /// <returns></returns>
        public static int GetConnectionInfoCount(SignalRClientType clientType, string clientFlag, ConnectionState connectionState)
        {
            return connectionMap.Values.Count(conn => conn.ClientType == clientType && conn.ClientFlag == clientFlag);
        }
        /// <summary>
        /// 获取连接信息
        /// </summary>
        /// <param name="clientType">客户端类型</param>
        /// <param name="clientFlag">客户端标记</param>
        /// <returns></returns>
        public static List<ConnectionInfo> GetConnectionInfo(SignalRClientType clientType, string clientFlag)
        {
            var connections = connectionMap.Values.Where(conn => conn.ClientType == clientType && conn.ClientFlag == clientFlag);
            return connections.ToList();
        }
    }
}
