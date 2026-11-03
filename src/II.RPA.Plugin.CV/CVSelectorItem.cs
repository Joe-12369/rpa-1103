using System;
using System.Collections.Generic;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using II.RPA.Logging;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.CV
{
    internal class CVSelectorItem : SelectorItem
    {
        private static ILogger logger = Logger.OfContext();

        public CVSelectorItem(CVSelectorItem item)
        {
            this.SetBackingFieldValues(item._backingFieldValues);
            this.Properties = item.Properties;
            this.Element = item.Element;
            this.ElementName = item.ElementName;
        }

        public CVSelectorItem(AutomationElement element, int x = 0, int y = 0, int width = 0, int height = 0, int indexInParent = -1, string elementObjStr = "", string anchorObjStr = "")
        {
            this.Element = new CVNode(element);
            this.ElementName = element?.ControlType.ToString();
            this.Properties = new List<SelectorItemProperty>();

            if (string.IsNullOrEmpty(this.ElementName))
            {
                this.ElementName = ConstFields.SELECTOR_CV_ELEMENT;
            }
            try
            {
                if (element != null)
                {
                    if (element.Properties.Name.IsSupported && !string.IsNullOrEmpty(element.Properties.Name.Value))
                    {
                        this.Properties.Add(new CVSelectorItemProperty(ConstFields.SELECTOR_NAME, element.Properties.Name.Value));
                    }
                    if (element.Properties.ClassName.IsSupported && !string.IsNullOrEmpty(element.Properties.ClassName))
                    {
                        this.Properties.Add(new CVSelectorItemProperty(ConstFields.SELECTOR_CLASS_NAME, element.Properties.ClassName.Value));
                    }
                }
                if (indexInParent > -1)
                {
                    this.Properties.Add(new SelectorItemProperty(ConstFields.SELECTOR_INDEX_IN_PARENT, indexInParent.ToString()));
                }

                if (!(x == 0 && y == 0))
                {
                    this.Properties.Add(new CVSelectorItemProperty(ConstFields.SELECTOR_ELEMENT_OBJ, elementObjStr));
                    this.Properties.Add(new CVSelectorItemProperty(ConstFields.SELECTOR_ANCHOR_OBJ, anchorObjStr));
                }
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "CV Exception:");
            }
            foreach (CVSelectorItemProperty p in this.Properties)
            {
                p.Accurate = true;
                p.Enabled = true;
                p.CanDisable = (this.Properties.Count > 1);
            };
            Enabled = true;
            CanDisable = true;
        }

        protected CVSelectorItem()
        { }

        public string ElementName { get; set; }

        public override JObject json
        {
            get
            {
                SelectorProps selectorProp = new SelectorProps()
                {
                    Name = this.ElementName,
                    Props = this.Properties.OfType<CVSelectorItemProperty>().Where(x => x.Enabled).Select(x => x.Convert2Props()).ToList()
                };

                return JObject.FromObject(selectorProp);
            }
        }

        public static CVSelectorItem FromJObject(JObject o)
        {
            CVSelectorItem selectorItem = new CVSelectorItem();
            selectorItem.ElementName = o[ConstFields.SELECTOR_ELEMENT_NAME_TAG].ToString();
            //var props = o[ConstFields.SELECTOR_ELEMENT_PROPS_TAG];
            var props = o[ConstFields.SELECTOR_ELEMENT_PROPS_TAG];
            try
            {
                var accurate = o["accurate"].ToString();
                if (!string.IsNullOrEmpty(accurate) && accurate == "false")
                    return null;
            }
            catch
            {

            }

            var selectItemProperties = props.ToObject<List<CVSelectorItemProperty>>();
            selectorItem.Properties = new List<SelectorItemProperty>(selectItemProperties);
            selectorItem.Enabled = true;
            return selectorItem;
        }

        /// <summary>
        /// 获取不带通配符的匹配条件
        /// </summary>
        /// <param name="isExistStarProp">是否存在通配符的情况</param>
        /// <returns>不带通配符的条件</returns>
        internal AndCondition GenerateConditionsWithoutStar(out bool isExistStarProp, ref bool isSapWindow)
        {
            var automation = UiCommon.UIAAutomation;
            var cond = new List<ConditionBase>();
            isExistStarProp = this.Properties.Exists(x => x.Enabled && (!string.IsNullOrEmpty(x.Value) && x.Value.Contains("*")));
            var commonProps = this.Properties.OfType<CVSelectorItemProperty>().Where(x => x.Enabled && (!string.IsNullOrEmpty(x.Value) && !x.Value.Contains("*")));

            ControlType ct = FlaUI.Core.Definitions.ControlType.Unknown;

            if (Enum.TryParse<ControlType>(this.ElementName, out ct))
            {
                cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ControlType, ct));
            }
            else
            {
                cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ControlType, FlaUI.Core.Definitions.ControlType.Unknown));
            }

            foreach (var p in commonProps)
            {
                string propName = p.Name;
                string propValue = p.Value;

                if (p.Pattern.Equals(ConstFields.PATTERN_EQUAL) && propName.Equals(ConstFields.SELECTOR_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.Name, propValue));
                }
                else if (propName.Equals(ConstFields.SELECTOR_CLASS_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ClassName, propValue));
                    if (propValue == "SAP_FRONTEND_SESSION" && ct == ControlType.Window)
                    {
                        isSapWindow = true;
                    }
                }
                else if (propName.Equals(ConstFields.SELECTOR_AUTOMATION_ID, StringComparison.OrdinalIgnoreCase))
                {
                    cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.AutomationId, propValue));
                }
            }

            return new AndCondition(cond);
        }
    }
}
