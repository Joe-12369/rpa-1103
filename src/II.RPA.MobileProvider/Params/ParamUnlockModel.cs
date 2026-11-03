using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamUnlockModel
    {
        public UnlockType unlockType { get; set; }
        public string unlockPw { get; set; }
        /// <summary>
        /// 解锁超时时间（秒）
        /// </summary>
        public int unlockTimeout { get; set; } = 30;
    }
}
