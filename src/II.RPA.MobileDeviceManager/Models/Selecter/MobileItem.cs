using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Models
{
    [Serializable]
    public  class MobileItem
    {
        public string name { get; set; }
        public string type { get; set; }
        public string accurate { get; set; }
        public List<Props> props { get; set; }
    }
    [Serializable]
    public class Props
    {
        public string name { get; set; }
        public string value { get; set; }
        public string pattern { get; set; }
        public string accurate { get; set; }
    }
}
