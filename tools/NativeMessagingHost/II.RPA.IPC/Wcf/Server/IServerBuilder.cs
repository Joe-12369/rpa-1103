using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.IPC
{
    /// <summary>
    /// 服务端构造器，支持自定服务模式构造
    /// </summary>
    public interface IServerBuilder
    {
        IServer Build();
    }
}
