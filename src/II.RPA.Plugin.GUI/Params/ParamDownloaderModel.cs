using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamDownloaderModel : ParamBase
    {
        public string saveFile { get; set; }
        public string fileName { get; set; }
        public int interval { get; set; }
        /// <summary>
        /// 保存模式
        /// 1:生成副本  2:替换  3:跳过
        /// </summary>
        public int saveMode { get; set; }
        /// <summary>
        /// 输入模式
        /// 0:后台接口 1:模拟输入 2:剪切板粘贴
        /// </summary>
        public int inputMode { get; set; }
        /// <summary>
        /// 下载超时
        /// </summary>
        public int timeout { get; set; } = 10000;
        /// <summary>
        /// 点击模式
        /// 0：后台操作，1：模拟操作
        /// </summary>
        public int clickMode { get; set; }

        public int waitDialogTime { get; set; } = 30000;
    }
}
