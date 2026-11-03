using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Database
{
    public class DbServerConnStr
    {
        public string DataSource { get; set; }
        public string DbName { get; set; }
        public string UserName { get; set; }
        public string Pwd { get; set; }
        public string Charset { get; set; }
        public string Port { get; set; }
        public string Sid { get; set; }
        public string Filepath { get; set; }
        public string DSN { get; set; }
        public EmServerType ServerType { get; set; }
    }

    public enum EmServerType
    {
        SID = 0,
        SERVICE_NAME = 1,
    }
}
