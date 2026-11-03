using II.RPA.Plugin.GUI.Commons.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.SAP.Controls
{
    interface ITable
    {
        List<List<string>> GetTableData(string id, ParamTableModel tm);
    }
}
