using System;

namespace II.RPA.Plugin.Excel.Filter.Executing
{
    internal class ReadContentFilter : ActionFilter
    {
        public ReadContentFilter(FilterType filterType, int order = 0) : base(filterType, order)
        {
        }

        public override void OnExecution(ActionContext context)
        {
            if (context.ActionName.StartsWith("Read"))
            {
                if (context.ActionName.EndsWith("ReadContentV3"))
                {
                    context["__redirectAction"] = "ReadContent";
                    //调用来自【读取Excel内容】合成组件，但是重定向之前的组件调用
                    context["fromContent"] = true;
                    //控制是否获取隐藏的行
                    bool ignoreHidden = Convert.ToBoolean(context["ignoreHidden"]);
                    context["visible"] = ignoreHidden;

                    var option = Convert.ToInt32(context["excelRng"]["option"]);
                    if (option == 0)
                    {
                        context.ActionName = "ReadCell";
                        context["column"] = context["excelRng"]["column"];
                        context["row"] = context["excelRng"]["row"];
                    }
                    else if (option == 1)
                    {
                        context.ActionName = "ReadRow";
                        context["rowIndex"] = context["excelRng"]["row"];
                    }
                    else if (option == 2)
                    {
                        context.ActionName = "ReadColumn";
                        context["columnName"] = context["excelRng"]["column"];
                    }
                    else
                    {
                        context.ActionName = "ReadRangeV2";
                        context["range"] = context["excelRng"];
                    }
                }
                else
                {
                    //早期版本预留，一直未使用该属性。712 版本设计了该功能（组件属性变为 ignoreHidden)
                    context["visible"] = Equals(context["visible"], 1) || Equals(context["visible"], true) || Equals(context["visible"], "是");
                    //decimal 属性早期产品设计用来控制读取值是否包含浮点数，高版本用valueType 替代，兼容 decimal 属性
                    //0:读取Value2 属性，1读取Text属性 2读取Value属性
                    context["valueType"] = context["decimal"];
                }

            }

        }
    }
}
