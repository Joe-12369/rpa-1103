using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public class MouseParamDef
    {
        #region 鼠标点击位置
        public const int CUR_POS_CENTER = 0;
        public const int CUR_POS_LEFT_TOP = 1;
        public const int CUR_POS_RIGHT_TOP = 2;
        public const int CUR_POS_LEFT_BOTTOM = 3;
        public const int CUR_POS_RIGHT_BOTTOM = 4;
        public const int CUR_POS_RANDOM = 5;
        public const int CUR_POS_CENTER_TOP = 6;
        public const int CUR_POS_CENTER_LEFT = 7;
        public const int CUR_POS_CENTER_RIGHT = 8;
        public const int CUR_POS_CENTER_BOTTOM = 9;
        #endregion

        #region 鼠标按键类型
        /// <summary>
        /// 鼠标左键
        /// </summary>
        public const int CLK_BTN_LEFT = 0;
        /// <summary>
        /// 鼠标右键
        /// </summary>
        public const int CLK_BTN_RIGHT = 1;
        /// <summary>
        /// 鼠标中键
        /// </summary>
        public const int CLK_BTN_CENTER = 2;
        #endregion

        #region 点击类型
        /// <summary>
        /// 单击
        /// </summary>
        public const int CLK_TYPE_SINGLE = 0;
        /// <summary>
        /// 双击
        /// </summary>
        public const int CLK_TYPE_DOUBLE = 1;
        /// <summary>
        /// 三击
        /// </summary>
        public const int CLK_TYPE_TRIPLE = 2;
        /// <summary>
        /// 按下
        /// </summary>
        public const int CLK_TYPE_PRESS = 3;
        /// <summary>
        /// 松开
        /// </summary>
        public const int CLK_TYPE_RELEASE = 4;
        #endregion

        #region 操作类型
        /// <summary>
        /// 后台操作
        /// </summary>
        public const int BACKGROUND_OPT = 0;
        /// <summary>
        /// 模拟操作
        /// </summary>
        public const int SIMULATE_OPT = 1;
        #endregion
    }
}
