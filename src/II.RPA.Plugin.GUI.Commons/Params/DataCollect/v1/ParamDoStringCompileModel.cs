using II.RPA.Plugin.Core.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public class ParamDoStringCompileModel
    {
        /// <summary>
        /// 要编译的字符
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// 参数序列化本文
        /// </summary>
        public string ParamsStr { get; set; }
    }
    public class ParamSelectorValueAddStrEscapeModel
    {
        public List<SelectorProps> selector { get; set; }
    }
    public class ParamSelectorValueRemoveStrEscapeModel
    {
        public List<SelectorProps> selector { get; set; }
    }
}
