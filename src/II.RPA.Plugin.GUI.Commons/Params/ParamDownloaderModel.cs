using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public enum InputMode
    {
        SetValue,
        Simulate,
        ClipBoard
    }
    public enum ClickMode
    {
        API = 0,
        Simulate = 1
    }
    public enum SaveMode
    {
        None,
        Copy,
        Replace,
        Skip
    }
    public class ParamDownloaderModel
    {
        public InputMode InputMode
        {
            get
            {
                return (InputMode)Enum.ToObject(typeof(InputMode), this.inputMode);
            }
        }
        public ClickMode ClickMode
        {
            get
            {
                return (ClickMode)this.clickMode;
            }
        }
        public SaveMode SaveMode
        {
            get
            {
                return (SaveMode)Enum.ToObject(typeof(SaveMode), this.saveMode);
            }
        }
        public int interval { get; set; }
        public string saveFile { get; set; }
        public string fileName { get; set; }
        /// <summary>
        /// 保存模式
        /// 1:生成副本  2:替换  3:跳过
        /// </summary>
        public int saveMode { get; set; } = 2;
        /// <summary>
        /// 输入模式
        /// 0:后台接口 1:模拟输入 2:剪切板粘贴
        /// </summary>
        public int inputMode { get; set; } = 1;
        /// <summary>
        /// 点击模式
        /// 0：后台操作，1：模拟操作
        /// </summary>
        public int clickMode { get; set; } = 1;
        /// <summary>
        /// 下载超时
        /// </summary>
        public int timeout { get; set; } = 10000;

        public int waitDialogTime { get; set; } = 30000;
        public int buttonType { get; set; }
    }
    public enum UploadType
    {
        File,
        Folder
    }
    public class ParamFileUploaderModel
    {
        public InputMode InputMode
        {
            get
            {
                return (InputMode)Enum.ToObject(typeof(InputMode), this.inputMode);
            }
        }
        public int interval { get; set; }
        /// <summary>
        /// 输入模式
        /// 0:后台接口 1:模拟输入 2:剪切板粘贴
        /// </summary>
        public int inputMode { get; set; } = 1;
        public int waitDialogTime { get; set; } = 30000;
        public UploadType uploadType { get; set; }
        public string filePath { get; set; }
        public int buttonType { get; set; }
    }
}
