using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.EventMonitor.Common
{
    internal class Configurations
    {
        private static IDictionary<int, string> dic_error_info = null;
        private readonly object obj = new object();

        static Configurations()
        {
            dic_error_info = new Dictionary<int, string>();
            dic_error_info.Add(0, "成功");
            dic_error_info.Add(1001, "开始（添加）触发器任务成功");
            dic_error_info.Add(1002, "关闭（移除）触发器任务成功");
            dic_error_info.Add(1003, "触发器触发成功");
            dic_error_info.Add(1004, "校验成功");

            dic_error_info.Add(4001, "开始（添加）触发器任务失败");
            dic_error_info.Add(40010101, "开始（添加）触发器任务失败 - 入参解析异常");
            dic_error_info.Add(4002, "关闭（移除）触发器任务失败");
            dic_error_info.Add(40020101, "关闭（移除）文件触发器任务失败");
            dic_error_info.Add(40020201, "关闭（移除）邮件触发器任务失败");
            dic_error_info.Add(40020301, "关闭（移除）按键触发器任务失败");
            dic_error_info.Add(40020401, "关闭（移除）鼠标触发器任务失败");
            dic_error_info.Add(4003, "触发器任务执行过程中发生的未要求明确场景的失败情况");
            dic_error_info.Add(40030101, "文件触发器执行异常 - 其他异常");
            dic_error_info.Add(40030102, "文件触发器执行异常 - 入参无效");
            dic_error_info.Add(40030103, "文件触发器执行异常 - 文件路径不存在");
            dic_error_info.Add(40030104, "文件触发器执行异常 - 事件处理过程中发生异常，详情查看日志");
            dic_error_info.Add(40030201, "邮件触发器执行异常 - 其他异常");
            dic_error_info.Add(40030202, "邮件触发器执行异常 - 入参无效");
            dic_error_info.Add(40030203, "邮件触发器执行异常 - 邮箱参数配置错误");
            dic_error_info.Add(40030204, "邮件触发器执行异常 - 邮件触发器执行异常-网络异常");
            dic_error_info.Add(40030301, "按键触发器执行异常 - 其他异常");
            dic_error_info.Add(40030302, "按键触发器执行异常 - 入参无效");
            dic_error_info.Add(40030401, "鼠标触发器执行异常 - 其他异常");
            dic_error_info.Add(40030402, "鼠标触发器执行异常 - 入参无效");
            dic_error_info.Add(4004, "触发器配置校验失败");
            dic_error_info.Add(40040201, "触发器配置校验失败 - 邮件触发器");

        }

        internal static string GetErrorMessge(eMessageStatus status)
        {
            return GetErrorMessge((int)status);
        }
        internal static string GetErrorMessge(int status)
        {
            if (dic_error_info.ContainsKey(status))
            {
                return dic_error_info[status];
            }
            return "";
        }


    }
}
