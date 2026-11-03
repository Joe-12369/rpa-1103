using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Deeps.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace II.RPA.Plugin.GUI.Deeps
{
    public static class ElementPropertiesProvider
    {
        private readonly static HashSet<string> IgnorePropertyNames = new HashSet<string>()
        {
            "NativeElement",
            "NativeElement1",
            "NativeElement2",
            "NativeElement3",
            "NativeElement4",
            "NativeElement5",
            "NativeElement6",
            "NativeElement7",
            "NativeElement8",
            "NativeElement9",
            "Automation",
            "Properties",
        };

        private readonly static List<string> OrderPropertyNames = new List<string>()
        {
           "Name",
           "ClassName",
           "NativeWindowHandle",
           "LocalizedControlType",
           "ControlType",
           "BoundingRectangle",
           "FrameworkId",
           "IsEnabled",
           "IsOffscreen",
           "ProcessId",
        };

        public static List<ElementPropertyDto> ToDtoList(AutomationElement element)
        {
            var list = new List<ElementPropertyDto>();
            if (element == null) return list;

            var props = element.Properties;
            var type = props.GetType();

            foreach (var propInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                try
                {
                    if (IgnorePropertyNames.Contains(propInfo.Name))
                        continue;

                    var automationProperty = propInfo.GetValue(props);
                    if (automationProperty == null) continue;

                    var isSupportedProp = automationProperty.GetType().GetProperty("IsSupported");
                    var valueOrDefaultProp = automationProperty.GetType().GetProperty("ValueOrDefault");

                    bool isSupported = (bool)(isSupportedProp?.GetValue(automationProperty) ?? false);

                    string valueStr;
                    if (!isSupported)
                    {
                        valueStr = "";
                    }
                    else
                    {
                        var valueObj = valueOrDefaultProp?.GetValue(automationProperty);

                        if (valueObj == null)
                        {
                            valueStr = null;
                        }
                        else if (valueObj is Array arr)
                        {
                            valueStr = string.Join(",", arr.Cast<object>());
                        }
                        else
                        {
                            valueStr = valueObj.ToString();
                        }

                    }

                    list.Add(new ElementPropertyDto
                    {
                        Key = propInfo.Name,
                        Value = valueStr
                    });
                }
                catch
                {
                    
                }
            }

            var ordered = list
                .OrderBy(dto =>
                {
                    var index = OrderPropertyNames.IndexOf(dto.Key);
                    return index >= 0 ? index : int.MaxValue;
                })
                .ToList();

            return ordered;
        }

        public static string BuildPropertiesJson(ElementModel element)
        {
            var dtoList = ToDtoList(element.Automation);
            var detail = new ElementDetailDto
            {
                ElementId = element.Id,
                Meta = dtoList,
                ElementAlias = GetAlias(element.Automation)
            };
            return JsonConvert.SerializeObject(detail);
        }

        public static string GetAlias(AutomationElement element)
        {
            string alias = element.Properties.Name.ValueOrDefault;

            if (string.IsNullOrEmpty(alias))
            {
                alias = element.ControlTypeName;
            }
            else
            {
                if (!string.IsNullOrEmpty(element.ControlTypeName))
                {
                    alias = $"{element.ControlTypeName.ToLower()}_{alias}";
                }
            }

            if (string.IsNullOrEmpty(alias))
            {
                alias = element.Properties.ClassName.ValueOrDefault;
            }

            if (!string.IsNullOrEmpty(alias) && alias.Length > 20)
            {
                alias = alias.Substring(0, 20);
            }

            return alias ?? "element";
        }

    }

}
