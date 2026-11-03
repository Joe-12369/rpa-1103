using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace II.RPA.Plugin.Database.Params
{
    public class ParamCreateDbConnectionModel : ParamBase
    {
        public EmDbType DbType { get; set; }

        public string ConnectStr { get; set; }
    }

    public class KeyDbConnection
    {
        public ParamCreateDbConnectionModel DbConnectionModel { get; set; }

        public string Key { get; set; }

        public EmDbConnectStatus DbConnectStatus { get; set; } = EmDbConnectStatus.Open;

        public override string ToString()
        {
            return EncryptUtils.Base64Encrypt(JsonConvert.SerializeObject(this));
        }

        public KeyDbConnection() { }

        public KeyDbConnection(string connectStr)
        {
            string str = EncryptUtils.Base64Decrypt(connectStr);
            var value = JsonConvert.DeserializeObject<KeyDbConnection>(str);
            if (value != null)
            {
                Key = value.Key;
                DbConnectionModel = value.DbConnectionModel;
                DbConnectStatus = value.DbConnectStatus;
            }
        }
    }
}
