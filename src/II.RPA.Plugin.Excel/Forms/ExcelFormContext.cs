using System.Drawing;

namespace II.RPA.Plugin.Excel.Forms
{
    public class ExcelFormContext
    {
        /// <summary>
        /// 版本
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// 窗体扩展参数
        /// </summary>
        public dynamic Arguments { get; set; }
        /// <summary>
        /// 窗体标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 提示信息
        /// </summary>
        public string InfoMessage { get; set; }
        /// <summary>
        /// 操作的Excel对象
        /// </summary>
        public string ExcelObject { get; set; }
        /// <summary>
        /// 提示文本
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// 错误提示消息
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// 获取Excel值
        /// </summary>
        public dynamic Value { get; set; } = "";
        /// <summary>
        /// 提示图片
        /// </summary>
        public string Image { get; set; }
    }
}
