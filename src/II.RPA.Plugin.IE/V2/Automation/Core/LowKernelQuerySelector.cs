using II.RPA.Plugin.Core.Commons;
using MSHTML;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using System.Linq;
using II.RPA.Plugin.IE.V2.Automation.Provider;

namespace II.RPA.Plugin.IE.V2.Automation.Core
{
    internal class LowKernelQuerySelector
    {
        private static ILogger logger = Logger.OfContext();
        private static LowKernelQuerySelector lowKernelQuerySelector;
        private static readonly object locker = new object();
        private LowKernelQuerySelector()
        {
        }

        public static LowKernelQuerySelector GetInstance()
        {
            if (lowKernelQuerySelector == null)
            {
                lock (locker)
                {
                    if (lowKernelQuerySelector == null)
                    {
                        lowKernelQuerySelector = new LowKernelQuerySelector();
                    }
                }
            }
            return lowKernelQuerySelector;
        }

        private List<IElementSelector> QueryDocumentBody(IHTMLDocument2 doc2)
        {
            List<IElementSelector> listeleSelectors = new List<IElementSelector>();
            IHTMLElement docElement = doc2.body as IHTMLElement;
            if (docElement.tagName.ToLower() == "body")
            {
                var elementSelector = doc2.body as IElementSelector;
                listeleSelectors.Add(elementSelector);
            }
            else
            {
                IHTMLDocument3 doc3 = doc2 as IHTMLDocument3;
                IHTMLDOMChildrenCollection eles = doc3.childNodes as IHTMLDOMChildrenCollection;
                for (int i = 0; i < eles.length; i++)
                {
                    IHTMLElement ele = eles.item(i) as IHTMLElement;
                    var elementSelector = ele as IElementSelector;
                    listeleSelectors.Add(elementSelector);
                }
            }
            return listeleSelectors;
        }
        /// <summary>
        /// ElementID Find Element
        /// </summary>
        /// <param name="listeleSelectors"></param>
        /// <param name="tagName"></param>
        /// <param name="prop"></param>
        /// <returns></returns>
        private List<IElementSelector> CollectQuerySelectorAllByID(List<IElementSelector> listeleSelectors, string tagName, Props prop)
        {
            List<IElementSelector> eleSelectors = new List<IElementSelector>();
            foreach (var item in listeleSelectors)
            {
                IHTMLElement element = item as IHTMLElement;
                IHTMLDocument doc = element.document as IHTMLDocument;
                IHTMLDocument3 doc3 = doc as IHTMLDocument3;
                IHTMLElement e = doc3.getElementById(prop.Value);
                if (e != null && e.tagName.ToLower() == tagName)
                    eleSelectors.Add(e as IElementSelector);
            }
            return eleSelectors;
        }

        private string ConvertCssSelector(Props prop, string tagName)
        {
            if (prop.Accurate.As<bool>())
            {
                if (prop.Name == "id" && prop.Pattern == "equal")
                {
                    return tagName + "#" + prop.Value;
                }
                else if (prop.Name == "class" && prop.Pattern == "equal")
                {
                    return $"{tagName}.{prop.Value.Replace(" ", ".")}";
                }
                else if (prop.Name != "@index" && prop.Pattern == "equal")
                {
                    return $"{tagName}[{prop.Name}='{prop.Value}']";
                }
            }
            return string.Empty;
        }
        private string GetAttribute(IHTMLElement element, Props prop)
        {
            string attributeValue = String.Empty;
            if (prop.Name == "class")
                attributeValue = element.className;
            else if (prop.Name == "innertext")
                attributeValue = element.innerText;
            else if (prop.Name == "@index")
            {
                var index = Utils.GetNthChild(element) + 1;
                attributeValue = index.ToString();
            }
            else if (prop.Name == "src")
            {
                var tags = new string[] { "iframe", "frame" };
                if (tags.Contains(element.tagName.ToLower()))
                {
                    IHTMLIFrameElement3 frameelement = element as IHTMLIFrameElement3;
                    if (frameelement != null)
                        attributeValue = frameelement.src;
                    else
                        attributeValue = element.getAttribute(prop.Name) as string;
                }
                else
                {
                    attributeValue = element.getAttribute(prop.Name) as string;
                }
            }
            else
                attributeValue = element.getAttribute(prop.Name) as string;
            logger.LogDebug(prop.Name + " attributeValue: " + attributeValue);
            return attributeValue;
        }
        private bool MatchCondition(string tagName, IHTMLElement element, Props prop)
        {
            if (element != null && element.tagName.ToLower() == tagName)
            {
                var attributeValue = GetAttribute(element, prop);
                if (prop.Pattern == "equal")
                {
                    if (!string.IsNullOrEmpty(attributeValue) && prop.Value == attributeValue)
                    {
                        return true;
                    }
                }
                else if (prop.Pattern == "contain")
                {
                    if (!string.IsNullOrEmpty(attributeValue) && attributeValue.Contains(prop.Value))
                    {
                        return true;
                    }
                }
                else if (prop.Pattern == "begin")
                {
                    if (!string.IsNullOrEmpty(attributeValue) && Regex.IsMatch(attributeValue, @"^" + prop.Value))
                    {
                        return true;
                    }
                }
                else if (prop.Pattern == "end")
                {
                    if (!string.IsNullOrEmpty(attributeValue) && Regex.IsMatch(attributeValue, prop.Value + @"$"))
                    {
                        return true;
                    }
                }
                else if (prop.Pattern == "regular")
                {
                    if (!string.IsNullOrEmpty(attributeValue) && Regex.IsMatch(attributeValue, prop.Value))
                    {
                        return true;
                    }
                }
                else if (prop.Pattern == "exclude")
                {
                    if (prop.Name == "class")
                    {
                        if (!string.IsNullOrEmpty(attributeValue) && attributeValue != prop.Value)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// cssSelector Find Element
        /// </summary>
        /// <param name="listeleSelectors"></param>
        /// <param name="cssSelector"></param>
        /// <returns></returns>
        private List<IElementSelector> CollectQuerySelectorAll(List<IElementSelector> listeleSelectors, string cssSelector)
        {
            List<IElementSelector> eleSelectors = new List<IElementSelector>();
            foreach (var item in listeleSelectors)
            {
                try
                {
                    IHTMLDOMChildrenCollection elementCollection = item.querySelectorAll(cssSelector);
                    for (int nthIndex = 0; nthIndex < elementCollection.length; nthIndex++)
                    {
                        IHTMLElement e = elementCollection.item(nthIndex) as IHTMLElement;
                        var elementSelector = e as IElementSelector;
                        eleSelectors.Add(elementSelector);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }
            return eleSelectors;
        }
        /// <summary>
        /// Find Element Children
        /// </summary>
        /// <param name="listeleSelectors"></param>
        /// <returns></returns>
        private List<IElementSelector> CollectChildren(List<IElementSelector> listeleSelectors)
        {
            List<IElementSelector> eleSelectors = new List<IElementSelector>();
            foreach (var item in listeleSelectors)
            {
                IHTMLElement element = item as IHTMLElement;
                IHTMLElementCollection elementCollection = element.children;
                for (int nthIndex = 0; nthIndex < elementCollection.length; nthIndex++)
                {
                    IHTMLElement e = elementCollection.item(nthIndex) as IHTMLElement;
                    var elementSelector = e as IElementSelector;
                    eleSelectors.Add(elementSelector);
                }
            }
            return eleSelectors;
        }
        private List<IElementSelector> CollectQuerySelectorAllOrChildren(List<IElementSelector> listeleSelectors, string tagName)
        {
            List<IElementSelector> eleSelectors = new List<IElementSelector>();
            //优先 querySelector ，拾取时不是全链路获取层级
            eleSelectors = CollectQuerySelectorAll(listeleSelectors, tagName);
            if (eleSelectors.Count == 0)
                eleSelectors = CollectChildren(listeleSelectors);
            return eleSelectors;
        }
        private List<IElementSelector> QueryCssSelectorAll(List<IElementSelector> listeleSelectors, string tagName, Props props)
        {
            List<IElementSelector> eleSelectors = new List<IElementSelector>();
            var cssSelector = ConvertCssSelector(props, tagName);
            logger.LogDebug($"QuerySelectorAll Prop cssSelector:{cssSelector}");
            if (!string.IsNullOrEmpty(cssSelector))
            {
                eleSelectors = CollectQuerySelectorAll(listeleSelectors, cssSelector);
                if (eleSelectors.Count == 0)
                {
                    if (props.Name == "id")
                    {
                        eleSelectors = CollectQuerySelectorAllByID(listeleSelectors, tagName, props);
                    }
                    else
                    {
                        eleSelectors = CollectQuerySelectorAllOrChildren(listeleSelectors, tagName);
                    }
                }
            }
            else
            {
                eleSelectors = CollectQuerySelectorAllOrChildren(listeleSelectors, tagName);
            }
            return eleSelectors;
        }
        private List<IElementSelector> Record(List<IElementSelector> eleSelectors, string tagName, SelectorProps nodeSelector)
        {
            List<IElementSelector> listeleSelectors = new List<IElementSelector>();
            for (int i = 0; i < eleSelectors.Count; i++)
            {
                foreach (var prop in nodeSelector.Props)
                {
                    if (prop.Accurate.As<bool>())
                    {
                        IHTMLElement ele = eleSelectors[i] as IHTMLElement;
                        if (MatchCondition(tagName, ele, prop))
                        {
                            if (!listeleSelectors.Contains(eleSelectors[i]))
                                listeleSelectors.Add(eleSelectors[i]);
                        }
                        else
                        {
                            if (listeleSelectors.Count != 0)
                            {
                                if (listeleSelectors.Contains(eleSelectors[i]))
                                    listeleSelectors.Remove(eleSelectors[i]);
                            }
                            break;
                        }
                    }
                }
            }
            return listeleSelectors;
        }
        internal List<IHTMLElement> QuerySelectorAll(string selector, IHTMLDocument2 doc2, string parentId)
        {
            List<string> paths = new List<string>();
            var propSelector = JsonConvert.DeserializeObject<List<SelectorProps>>(selector.ToString());
            List<IElementSelector> listeleSelectors = new List<IElementSelector>();
            if (!string.IsNullOrEmpty(parentId))
            {
                listeleSelectors.Add(Utils.ElementFromUniqueId(parentId, doc2 as HTMLDocument) as IElementSelector);
            }
            else
            {
                listeleSelectors = QueryDocumentBody(doc2);
            }
            List<IElementSelector> eleSelectors = new List<IElementSelector>();
            if (propSelector.Count() < 1)
            {
                return new List<IHTMLElement>();
            }
            foreach (var nodeSelector in propSelector)
            {
                var tagName = nodeSelector.Name;
                if (tagName == "tab")
                    continue;
                var props = nodeSelector.Props.Where(prop => prop.Accurate.As<bool>());
                if (listeleSelectors.Count == 0)
                    throw new RPAException(eErrorMessageType.Error_NotFound);
                if (nodeSelector.Accurate.As<bool>())
                {
                    if (props.Count() == 1)
                    {
                        var firstProp = props.FirstOrDefault();
                        eleSelectors = QueryCssSelectorAll(listeleSelectors, tagName, firstProp);
                    }
                    else
                    {
                        eleSelectors = CollectQuerySelectorAll(listeleSelectors, tagName);
                    }
                }
                else
                {
                    //跳级
                    eleSelectors = CollectChildren(listeleSelectors);
                }
                listeleSelectors = new List<IElementSelector>();

                if (props.Count() == 0 || !nodeSelector.Accurate.As<bool>() || eleSelectors.Count == 1)
                {
                    listeleSelectors = eleSelectors;
                    continue;
                }
                listeleSelectors = Record(eleSelectors, tagName, nodeSelector);
            }
            return listeleSelectors.ConvertAll(e => { return e as IHTMLElement; });
        }
    }
}
