using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamFileUploaderModel : ParamBase
    {
        #region 先和下载的参数保持一致吧
        /// <summary>
        /// 输入间隔
        /// </summary>
        public int interval { get; set; }
        /// <summary>
        /// 输入模式
        /// 0:后台接口 1:模拟输入 2:剪切板粘贴
        /// </summary>
        public int inputMode { get; set; }
        public int waitDialogTime { get; set; } = 30000;

        #endregion
        public UploadType uploadType { get; set; }
        public string filePath { get; set; }
        /// <summary>
        /// 上传文件超时
        /// </summary>
        public int timeout { get; set; } = 60000;
    }
    public enum UploadType
    {
        File,
        Folder
    }
}
