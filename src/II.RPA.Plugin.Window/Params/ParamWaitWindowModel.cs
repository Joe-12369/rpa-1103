using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Window.Params
{
    public class ParamWaitWindowModel : ParamBase
    {
        public EnumWaitType WaitType { get; set; }
    }
    public enum EnumWaitType
    {
        /// <summary>
        /// 出现
        /// </summary>
        Appear,
        /// <summary>
        /// 消失
        /// </summary>
        DisAppear,
        /// <summary>
        /// 激活
        /// </summary>
        Active
    }
}
