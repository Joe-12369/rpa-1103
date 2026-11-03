using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotLiteControls.UCControl
{
    /// <summary>
    /// 通用
    /// </summary>
    public interface IGetInputModelControl
    {
        /// <summary>
        /// 初始化控件
        /// </summary>
        /// <param name="input"></param>
        void Init(InputModel input);
        /// <summary>
        /// 获取控件的输入输出
        /// </summary>
        /// <returns></returns>
        InputModel GetInputModel();
    }
}
