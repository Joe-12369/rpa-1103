using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets.Params
{
    class ParamGetCellsDataModel
    {
        public string url { get; set; }
        public string startPosition { get; set; }
        public string endPosition { get; set; }
        public string sheetName { get; set; }
    }
}
