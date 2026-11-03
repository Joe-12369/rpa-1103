using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamInputTextModel : MobileElementValidationOptions
    {
        public string sessionId { get; set; }
        public string selector { get; set; }
        public string text { get; set; }
        /// <summary>
        /// 是否追加输入，0：非追加，1：追加输入
        /// </summary>
        public int appendInput { get; set; }
        public double timeout { get; set; }
        public int isEnter { get; set; } = 0;//0-回车 1-回车
        public int isVerify { get; set; } = 0;//0-不校验 1-校验
    }
}
