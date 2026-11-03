using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    public class JavaElement
    {
        public JavaElement() { }    
        public int ProcessId { get; set; }
        public JavaTech Tech { get; private set; }
        public string ClassName
        {
            get;set;
        }
        public string Text { get; set; }
        public int MatchedCount { get; set; } = 0;
        public string ControlType
        {
            get
            {
                if (!string.IsNullOrEmpty(MappedClazz) && MappedClazz.Contains('.'))
                {
                    return MappedClazz.Substring(MappedClazz.LastIndexOf('.') + 1);
                }
                return "";
            }
            private set { }
        }
        public Rectangle Rectangle { get; private set; }
        public JavaObjectWrapper JavaObject { get; private set; }
        /// <summary>
        /// 存储从ConvertSelectorToXPath得到的XPath，用于滚动后RefreshBoundingRectangle重新查询元素位置。
        /// FuncGetElementXpath返回的路径格式可能与FuncQueryElementFromXpath期望的格式不兼容。
        /// </summary>
        public string QueryPath { get; set; }
        public JavaElement(JavaObjectWrapper javaObject, Rectangle rectangle)
        {           
            Rectangle = rectangle;
            JavaObject = javaObject;
            MappedClazz = javaObject.GetClassName();
        }     
        public dynamic GetProperety(string propertyName)
        {
            return JavaObject.InvokeGetProperty(propertyName);
        }
        public void SetProperty(string propertyName, object value)
        {
            JavaObject.InvokeSetProperty(propertyName, new object[] { value });
        }
        public string GetElementPath()
        {
            var xpathRes = JavaObject.Proxy.CallFunction(Tech,FunctionId.FuncGetElementXpath, new object[] { JavaObject }, out CallStatus statusXpath);

            if (statusXpath == CallStatus.Ok && xpathRes != null && xpathRes.Length > 0)
            {
                return xpathRes[0] as string;

            }
            return null;
        }

        public object[] CallFunction(string eventName, object[] args)
        {           
            object[] callArgs;
            if (args == null || args.Length == 0)
            {
                callArgs = new object[] { JavaObject, eventName };
            }
            else
            {
                callArgs = new object[2 + args.Length];
                callArgs[0] = JavaObject;
                callArgs[1] = eventName;
                Array.Copy(args, 0, callArgs, 2, args.Length);
            }

            var callResult = JavaObject.Proxy.CallFunction(Tech, FunctionId.FuncTriggerEvent, callArgs, out CallStatus statusXpath);

            if (statusXpath == CallStatus.Ok)
            {
                return callResult;
            }           
            return null;
        }

        public string MappedClazz { get; private set; }
    }
}
