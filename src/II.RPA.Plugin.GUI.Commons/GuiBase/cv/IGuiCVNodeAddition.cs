using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiCVNodeAddition
    {
        JObject CVResultJson { get; set; }
        bool IsCVText { get; set; }
    }
}
