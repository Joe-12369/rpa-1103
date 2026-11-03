using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Excel.Models
{
    [MessagePackObject]
    public class SerializeData
    {
        [Key(0)]
        public int Rows { get; set; }

        [Key(1)]
        public int Columns { get; set; }

        [Key(2)]
        public dynamic[] Data { get; set; }
    }
}
