using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing
{
    internal interface IProcessing
    {

        int Order { get; }
        string Processing(string content, Commons.Params.DataCollect.ProcessingConfig config);
    }

}
