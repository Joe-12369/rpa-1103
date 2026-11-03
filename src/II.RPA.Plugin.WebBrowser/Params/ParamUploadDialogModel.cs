using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params
{
    public class ParamUploadDialogModel : ParamBase
    {
        /// <summary>
        /// 输入间隔
        /// </summary>
        public int interval { get; set; }
        /// <summary>
        /// 输入模式
        /// 0:后台接口 1:模拟输入 2:剪切板粘贴
        /// </summary>
        public int inputMode { get; set; }
        /// <summary>
        /// 等待对话框超时时间
        /// </summary>
        public int waitDialogTime { get; set; } = 30000;
        /// <summary>
        /// 上传类型 0-文件 1-文件夹
        /// </summary>
        public UploadType uploadType { get; set; }
        /// <summary>
        /// 文件(夹)路径
        /// </summary>
        public string filePath { get; set; }
        /// <summary>
        /// 上传文件超时
        /// </summary>
        public int timeout { get; set; } = 60000;
        /// <summary>
        /// 按钮类型
        /// </summary>
        public int buttonType { get; set; }

    }
    public enum UploadType
    {
        File,
        Folder
    }
}
