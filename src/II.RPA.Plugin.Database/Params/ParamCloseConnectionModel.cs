using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace II.RPA.Plugin.Database.Params
{
    public class ParamCloseConnectionModel : ParamBase
    {
        public string ConnectionID { get; set; }
    }
}
