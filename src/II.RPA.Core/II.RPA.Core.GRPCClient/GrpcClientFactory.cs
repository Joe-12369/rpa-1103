using Grpc.Core;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using Rpa.Core.Childsession;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPClient
{
    public class GrpcClientFactory
    {
        private readonly ChildSessionService.ChildSessionServiceClient childSessionServiceClient;
        private readonly Assembly currentAssembly;
        private readonly Dictionary<Type,object> clients;
        private readonly IEnumerable<Type> clientTypes;
        public GrpcClientFactory(ILogger aLogger)
        {
            currentAssembly = Assembly.GetExecutingAssembly();
            clients = new Dictionary<Type, object>();
            clientTypes = currentAssembly.GetTypes().Where(t => typeof(ClientBase).IsAssignableFrom(t));
        }
        public T CreateClient<T>(string anEndPoint) where T : ClientBase<T>
        {
            var tType = typeof(T);
            if (!clients.ContainsKey(tType))
            {
                var _channel = new Channel(anEndPoint, ChannelCredentials.Insecure);
                var obj = Activator.CreateInstance(tType, _channel) as T;
                clients.Add(tType, obj);
            }
            return clients[tType] as T;
        }
        public T CreateNewClient<T>(string anEndPoint) where T : ClientBase<T>
        {
            var tType = typeof(T);
            var _channel = new Channel(anEndPoint, ChannelCredentials.Insecure);
            return Activator.CreateInstance(tType, _channel) as T;
        }
    }
}
