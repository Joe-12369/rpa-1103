using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using II.RPA.Core.Provider.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction
{
    [ClientActionName("DBtest")]
    public class DBTestProvider : BaseActionProvider
    {
        public override string ActionName => "DBtest";
        private JObject m_ReqValResolvedJObject;
        private readonly string[] SQLiteSuffixes = new string[] { ".db", ".sqlite", ".db3" };
        private readonly string[] AccessSuffixes = new string[] { ".mdb", ".accdb", ".accdr" };
        /// <summary>
        /// sqlite and access
        /// </summary>
        private string m_DbFilePath;

        public DBTestProvider(ILogger aLogger) : base(aLogger)
        {

        }
        public DBTestProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {

        }

        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginName = "Database.TestDbConnection",
                PluginAction = "Excute",
            };
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            return base.ResolveRetVal(aRetValFromPlugin);
        }
        public override string ResolveReqVal(string aReqVal)
        {
            ///config和dType为前端固定的json属性
            JObject reqValObject = JObject.Parse(aReqVal);
            var vals = reqValObject.GetValue("config") as JArray;
            m_ReqValResolvedJObject = new JObject
            {
                { "DbType", reqValObject.GetValue("dType") }
            };
            JObject constr = new JObject();
            foreach (var val in vals)
            {
                if (val.Value<string>("name").Equals("Filepath"))
                {
                    m_DbFilePath = val.Value<string>("value");
                }
                if (val.Value<string>("name").Equals("Pwd"))
                {
                    try
                    {
                        Sm4Crypto sm = new Sm4Crypto();
                        sm.Key = ConfigurationManager.AppSettings["sm4key"];
                        sm.Data = val.Value<string>("value");
                        var valueStr = Sm4Crypto.DecryptECB(sm, Encoding.UTF8);
                        constr.Add(val.Value<string>("name"), valueStr);
                    }
                    catch (Exception)
                    {
                        byte[] valueBytes = Convert.FromBase64String(val.Value<string>("value"));
                        var valueStr = Encoding.UTF8.GetString(valueBytes);
                        constr.Add(val.Value<string>("name"), valueStr);
                    }
                }
                else
                {
                    constr.Add(val.Value<string>("name"), val.Value<string>("value"));
                }
            }
            m_ReqValResolvedJObject.Add("ConnectStr", constr.ToString());
            return m_ReqValResolvedJObject.ToString();
        }
        public override PluginRunResult PreResolveRetVal(string aReqVal,string aReqValOrigin = null)
        {
            //数据库种类
            //{
            //    MYSQL,
            //    SQLSERVER,
            //    ORACLA,
            //    PGSQL,
            //    SQLITE,
            //    ACCESS,
            //    DB2
            //}
            try
            {
                ///SQLITE
                if (m_ReqValResolvedJObject.Value<string>("DbType") == "4")
                {
                    if (string.IsNullOrEmpty(m_DbFilePath) 
                        || !SQLiteSuffixes.Contains(Path.GetExtension(m_DbFilePath)))
                    {
                        return new PluginRunResult
                        {
                            Code = "1",
                            Message = $"文件格式错误，不是标准的Sqlite数据库文件",
                            Result = m_DbFilePath ?? ""
                        };
                    }
                }
                ///ACCESS
                else if (m_ReqValResolvedJObject.Value<string>("DbType") == "5")
                {
                    if (string.IsNullOrEmpty(m_DbFilePath)
                        || !AccessSuffixes.Contains(Path.GetExtension(m_DbFilePath)))
                    {
                        return new PluginRunResult
                        {
                            Code = "1",
                            Message = $"文件格式错误，不是标准的Access数据库文件",
                            Result = m_DbFilePath ?? ""
                        };
                    }
                }
            }
            catch
            {
                ///ignore
            }
            return new PluginRunResult
            {
                Code = "0",
                Message = "go on",
                Result = ""
            };
               
        }
    }
}
