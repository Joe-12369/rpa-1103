using II.RPA.Plugin.Core.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public class ParamDoElementEditorModel
    {
        public string selector { get; set; }
        public string guiType { get; set; }
        public string filePath { get; set; }
        /// <summary>
        /// true:列相似元素拾取 校验时显示所有匹配元素 false:Table与翻页元素 校验时分别显示 整体表格与翻页按钮 均为一个高亮框
        /// </summary>
        public bool isSimilar { get; set; } = false;
    }
}
