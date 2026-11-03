using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotLiteControls.UCControl
{
    /// <summary>
    /// 有交互逻辑的控件
    /// </summary>
    public interface IRpaCustomControl : IGetInputModelControl
    {
        /// <summary>
        /// 获取控件的结果
        /// </summary>
        /// <returns></returns>
        string GetResult();
        /// <summary>
        /// 获取控件的输出(已停用)
        /// </summary>
        /// <returns></returns>
        OutputModel GetOutputModel();
        /// <summary>
        /// 设置上次的值，用于回显(已停用)
        /// </summary>
        /// <param name="model"></param>
        /// <param name="input"></param>
        void SetLast(OutputModel model, InputModel input);
    }
}
