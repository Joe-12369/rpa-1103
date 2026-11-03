using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Window.Params
{
    public class ParamWindowIsExistsModel : ParamBase
    {
        public int ExistsType { get; set; } //0-存在 1-不存在 
    }
}
