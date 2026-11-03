using System;

namespace II.RPA.Plugin.Excel.Filter.Executing
{
    internal class ActionParamsFilter : ActionFilter
    {
        public ActionParamsFilter(FilterType filterType, int order = 0) : base(filterType, order)
        {
        }
        public override void OnExecution(ActionContext context)
        {
            if (context.ActionParams.TryGetValue("workbook", out dynamic wb) && wb.StartsWith("_"))
            {
                context["workbook"] = wb.TrimStart('_');
            }

            //读取、写入组件名重定向
            if (context["fromContent"] != null && Equals(context["fromContent"], true))
            {
                context["__redirectAction"] = context.ActionName.StartsWith("Read") ? "ReadContent" : "WriteContent";
            }

            //删除全部图片选项
            if (context.ActionName.Contains("DeletePicture"))
            {
                context["__deleteAllPicture"] = context.ActionParams.TryGetValue("condition", out dynamic condtion) ? condtion : -1;
            }

            //是否是共享路径
            context["__IsShareFile"] = context.ActionParams.TryGetValue("share", out object value) && Convert.ToInt32(value) == 1;

            //是否是复制表格内容
            context["__IsCopyRangeV2"] = context.ActionParams.TryGetValue("CopyRangeV2", out dynamic _copyRngV2) && _copyRngV2 == 1;


        }
    }
}
