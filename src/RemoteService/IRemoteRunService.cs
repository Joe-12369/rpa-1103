using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace RemoteService
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的接口名“IRemoteRunService”。
    [ServiceContract]
    public interface IRemoteRunService
    {
        [OperationContract]
        Task DoWorkAsync(string user, string pw, string ip, string port);

        [OperationContract]
        Task CancelConnectionAsync(string user,string ip);
    }
}
