using II.RPA.Plugin.Core;
using MSHTML;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using II.RPA.Plugin.Core.Commons;
using Newtonsoft.Json.Linq;
using System.Linq;
using II.RPA.Logging.Formatting;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Plugin.IE.V2.Automation.Provider
{
    public static class Utils
    {
        private static ILogger logger = Logger.OfContext();
        private static RunJavaScript run = new RunJavaScript();
        public static string UNIQUE_KEY = "i-rpa";
        public static string injectStatus = string.Empty;
        public static int LastInjectIEVersion { get; set; }

        public static object ExecScript(IHTMLWindow2 window, string code)
        {
            return run.execScript(window, code);
        }


        //private static string Code_ID = "6472dd2def6f03fb76c8847e3d93ab08f2b0b946";
        public static void InjectJavaScript(IHTMLWindow2 window2)
        {
            try
            {
                var codeId = Utils.InvokeScript(window2, "window._iirpa_injectCodeID");
                if (string.IsNullOrEmpty(codeId) || !codeId.Equals(ExtensionProvider.Instance.CodeId))
                {
                    //Utils.injectStatus = Utils.InvokeScript(window2, "window._iirpa_injectstatus");
                    //if (string.IsNullOrEmpty(Utils.injectStatus))
                    {

                        Utils.ExecScript(window2, ExtensionProvider.Instance.Sizzle);
                        Utils.ExecScript(window2, ExtensionProvider.Instance.ContentIE);
                        //Utils.InvokeScript(window2, ExtensionProvider.Instance.Sizzle);
                        //Utils.InvokeScript(window2, ExtensionProvider.Instance.ContentIE);
                        var status = Utils.InvokeScript(window2, "IIRPA.api.injectStatus()");
                    }
                    GetIEVersion(window2, true);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "InjectJavaScript Error");
            }
        }
        public static int GetIEVersion(IHTMLWindow2 window2, bool realtime)
        {
            if (LastInjectIEVersion > 0 && !realtime)
            {
                return LastInjectIEVersion;
            }
            Stopwatch sp = Stopwatch.StartNew();
            Utils.ExecScript(window2, "IIRPA.getIeVersion()");
            var ieversion = Utils.InvokeScript(window2, "IIRPA.ieVersion");
            if (Convert.ToInt32(ieversion) < 9)
            {
                Utils.ExecScript(window2, ExtensionProvider.Instance.Json2);
                //Utils.InvokeScript(window2, ExtensionProvider.Instance.Json2);
                logger.LogInformation($"Inject JSON2");
            }
            LastInjectIEVersion = Convert.ToInt32(ieversion);
            sp.Stop();
            logger.LogInformation($"IE Version {ieversion},{sp.ElapsedMilliseconds}ms");

            return LastInjectIEVersion;
        }
        public static bool VerifyPraocessNameByHandle(IntPtr handle)
        {
            NativeMethods.GetWindowThreadProcessId(handle, out uint pId);
            Process process = Process.GetProcessById((int)pId);
            return process != null && process.ProcessName == "prevhost";
        }
        public static List<IElementSelector> ElementSelectorFindByID(List<IElementSelector> listeleSelectors, Props prop)
        {
            List<IElementSelector> eleSelectors = new List<IElementSelector>();
            foreach (var item in listeleSelectors)
            {
                IHTMLElement element = item as IHTMLElement;
                IHTMLDocument doc = element.document as IHTMLDocument;
                IHTMLDocument3 doc3 = doc as IHTMLDocument3;
                IHTMLElement e = doc3.getElementById(prop.Value);
                eleSelectors.Add(e as IElementSelector);
            }
            return eleSelectors;
        }
        public static List<IElementSelector> ElementSelectorFindChildren(List<IElementSelector> listeleSelectors, string cssSelector, string tagName, Props prop = null)
        {
            List<IElementSelector> eleSelectors = new List<IElementSelector>();
            foreach (var item in listeleSelectors)
            {
                logger.LogInformation($"ElementSelectorFindChildren cssSelector: " + cssSelector);
                IHTMLDOMChildrenCollection elementCollection = null;
                if (!string.IsNullOrEmpty(cssSelector))
                {
                    elementCollection = item.querySelectorAll(cssSelector);
                    logger.LogDebug($"ElementSelectorFindChildren elementCollection Count: " + elementCollection.length);
                    for (int nthIndex = 0; nthIndex < elementCollection.length; nthIndex++)//nth-child
                    {
                        IHTMLElement e = elementCollection.item(nthIndex) as IHTMLElement;
                        IHTMLElementCollection children = e.children as IHTMLElementCollection;
                        //logger.LogDebug(prop.Name + $" element QuerySelector ElementSelectorFindChildren: {LogBlock.NullCheck(e, "e")}");
                        logger.LogDebug("cssSelector: " + cssSelector);
                        for (int childIndex = 0; childIndex < children.length; childIndex++)
                        {
                            e = children.item(childIndex) as IHTMLElement;
                            if (e != null && e.tagName.ToLower() == tagName)
                            {
                                string attributeValue = String.Empty;
                                if (prop.Name == "class")
                                    attributeValue = e.className;
                                else if (prop.Name == "innertext")
                                    attributeValue = e.innerText;
                                else
                                    attributeValue = e.getAttribute(prop.Name) as string;
                                logger.LogDebug(prop.Name + "attributeValue: " + attributeValue);
                                if (prop.Pattern == "equal")
                                {
                                    if (!string.IsNullOrEmpty(attributeValue) && prop.Value == attributeValue)
                                    {
                                        var elementSelector = e as IElementSelector;
                                        eleSelectors.Add(elementSelector);
                                    }
                                }
                                else if (prop.Pattern == "contain")
                                {
                                    if (!string.IsNullOrEmpty(attributeValue) && attributeValue.Contains(prop.Value))
                                    {
                                        var elementSelector = e as IElementSelector;
                                        eleSelectors.Add(elementSelector);
                                    }
                                }
                                else if (prop.Pattern == "begin")
                                {
                                    if (!string.IsNullOrEmpty(attributeValue) && Regex.IsMatch(attributeValue, @"^" + prop.Value))
                                    {
                                        var elementSelector = e as IElementSelector;
                                        eleSelectors.Add(elementSelector);
                                    }
                                }
                                else if (prop.Pattern == "end")
                                {
                                    if (!string.IsNullOrEmpty(attributeValue) && Regex.IsMatch(attributeValue, prop.Value + @"$"))
                                    {
                                        var elementSelector = e as IElementSelector;
                                        eleSelectors.Add(elementSelector);
                                    }
                                }
                                else if (prop.Pattern == "regular")
                                {
                                    if (!string.IsNullOrEmpty(attributeValue) && Regex.IsMatch(attributeValue, prop.Value))
                                    {
                                        var elementSelector = e as IElementSelector;
                                        eleSelectors.Add(elementSelector);
                                    }
                                }
                                else if (prop.Pattern == "exclude")
                                {
                                    if (prop.Name == "class")
                                    {
                                        if (!string.IsNullOrEmpty(attributeValue) && attributeValue != prop.Value)
                                        {
                                            var elementSelector = e as IElementSelector;
                                            eleSelectors.Add(elementSelector);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    IHTMLElement ele = item as IHTMLElement;
                    IHTMLElementCollection children = ele.children as IHTMLElementCollection;
                    for (int childIndex = 0; childIndex < children.length; childIndex++)
                    {
                        IHTMLElement e = children.item(childIndex) as IHTMLElement;
                        //logger.LogDebug(prop.Name + $" element QuerySelector ElementSelectorFindChildren: {LogBlock.NullCheck(e, "e")}");
                        if (e != null && e.tagName.ToLower() == tagName)
                        {
                            string attributeValue = String.Empty;
                            if (prop != null)
                            {
                                if (prop.Name == "class")
                                    attributeValue = e.className;
                                else if (prop.Name == "innertext")
                                    attributeValue = e.innerText;
                                else
                                    attributeValue = e.getAttribute(prop.Name) as string;
                                logger.LogDebug(prop.Name + "attributeValue: " + attributeValue);
                                if (prop.Pattern == "equal")
                                {
                                    if (!string.IsNullOrEmpty(attributeValue) && prop.Value == attributeValue)
                                    {
                                        var elementSelector = e as IElementSelector;
                                        eleSelectors.Add(elementSelector);
                                    }
                                }
                                else if (prop.Pattern == "contain")
                                {
                                    if (!string.IsNullOrEmpty(attributeValue) && attributeValue.Contains(prop.Value))
                                    {
                                        var elementSelector = e as IElementSelector;
                                        eleSelectors.Add(elementSelector);
                                    }
                                }
                                else if (prop.Pattern == "begin")
                                {
                                    if (!string.IsNullOrEmpty(attributeValue) && Regex.IsMatch(attributeValue, @"^" + prop.Value))
                                    {
                                        var elementSelector = e as IElementSelector;
                                        eleSelectors.Add(elementSelector);
                                    }
                                }
                                else if (prop.Pattern == "end")
                                {
                                    if (!string.IsNullOrEmpty(attributeValue) && Regex.IsMatch(attributeValue, prop.Value + @"$"))
                                    {
                                        var elementSelector = e as IElementSelector;
                                        eleSelectors.Add(elementSelector);
                                    }
                                }
                                else if (prop.Pattern == "regular")
                                {
                                    if (!string.IsNullOrEmpty(attributeValue) && Regex.IsMatch(attributeValue, prop.Value))
                                    {
                                        var elementSelector = e as IElementSelector;
                                        eleSelectors.Add(elementSelector);
                                    }
                                }
                                else if (prop.Pattern == "exclude")
                                {
                                    if (prop.Name == "class")
                                    {
                                        if (!string.IsNullOrEmpty(attributeValue) && attributeValue != prop.Value)
                                        {
                                            var elementSelector = e as IElementSelector;
                                            eleSelectors.Add(elementSelector);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var elementSelector = e as IElementSelector;
                                eleSelectors.Add(elementSelector);
                            }
                        }
                    }
                }
            }
            return eleSelectors;
        }
        public static List<IElementSelector> ElementSelectorFindAll(List<IElementSelector> listeleSelectors, string cssSelector, string tagName, Props prop)
        {
            List<IElementSelector> eleSelectors = new List<IElementSelector>();
            foreach (var item in listeleSelectors)
            {
                logger.LogInformation($"ElementSelectorFindAll cssSelector: " + cssSelector);
                IHTMLDOMChildrenCollection elementCollection = item.querySelectorAll(cssSelector);
                logger.LogDebug($"ElementSelectorFindAll elementCollection Count: " + elementCollection.length);
                for (int nthIndex = 0; nthIndex < elementCollection.length; nthIndex++)//nth-child
                {
                    IHTMLElement e = elementCollection.item(nthIndex) as IHTMLElement;
                    //logger.LogDebug(prop.Name + $" element QuerySelector ElementSelectorFindAll: {LogBlock.NullCheck(e, "e")}");
                    if (e != null && e.tagName.ToLower() == tagName)
                    {
                        string attributeValue = String.Empty;
                        if (prop.Name == "class")
                            attributeValue = e.className;
                        else if (prop.Name == "innertext")
                            attributeValue = e.innerText;
                        else
                            attributeValue = e.getAttribute(prop.Name) as string;
                        logger.LogDebug(prop.Name + " attributeValue: " + attributeValue);
                        if (prop.Pattern == "equal")
                        {
                            if (!string.IsNullOrEmpty(attributeValue) && prop.Value == attributeValue)
                            {
                                var elementSelector = e as IElementSelector;
                                eleSelectors.Add(elementSelector);
                            }
                        }
                        else if (prop.Pattern == "contain")
                        {
                            if (!string.IsNullOrEmpty(attributeValue) && attributeValue.Contains(prop.Value))
                            {
                                var elementSelector = e as IElementSelector;
                                eleSelectors.Add(elementSelector);
                            }
                        }
                        else if (prop.Pattern == "begin")
                        {
                            if (!string.IsNullOrEmpty(attributeValue) && Regex.IsMatch(attributeValue, @"^" + prop.Value))
                            {
                                var elementSelector = e as IElementSelector;
                                eleSelectors.Add(elementSelector);
                            }
                        }
                        else if (prop.Pattern == "end")
                        {
                            if (!string.IsNullOrEmpty(attributeValue) && Regex.IsMatch(attributeValue, prop.Value + @"$"))
                            {
                                var elementSelector = e as IElementSelector;
                                eleSelectors.Add(elementSelector);
                            }
                        }
                        else if (prop.Pattern == "regular")
                        {
                            if (!string.IsNullOrEmpty(attributeValue) && Regex.IsMatch(attributeValue, prop.Value))
                            {
                                var elementSelector = e as IElementSelector;
                                eleSelectors.Add(elementSelector);
                            }
                        }
                        else if (prop.Pattern == "exclude")
                        {
                            if (prop.Name == "class")
                            {
                                if (!string.IsNullOrEmpty(attributeValue) && attributeValue != prop.Value)
                                {
                                    var elementSelector = e as IElementSelector;
                                    eleSelectors.Add(elementSelector);
                                }
                            }
                        }
                    }
                }
            }
            return eleSelectors;
        }

        public static List<IHTMLElement> FindIHTMLElement(string selector, IHTMLDocument2 doc2)
        {

            List<IHTMLElement> elements = new List<IHTMLElement>();
            var propSelector = JsonConvert.DeserializeObject<List<SelectorProps>>(selector.ToString());
            IHTMLElement docElement = doc2.body as IHTMLElement;
            IElementSelector elementSelector = null;
            List<string> paths = new List<string>();
            List<IElementSelector> eleSelectors = new List<IElementSelector>();
            List<IElementSelector> listeleSelectors = new List<IElementSelector>();
            try
            {


                if (docElement.tagName.ToLower() == "body")
                {
                    elementSelector = doc2.body as IElementSelector;
                    listeleSelectors.Add(elementSelector);
                }
                else
                {
                    IHTMLDocument3 doc3 = doc2 as IHTMLDocument3;
                    IHTMLDOMChildrenCollection eles = doc3.childNodes as IHTMLDOMChildrenCollection;

                    for (int i = 0; i < eles.length; i++)
                    {
                        IHTMLElement ele = eles.item(i) as IHTMLElement;
                        elementSelector = ele as IElementSelector;
                        listeleSelectors.Add(elementSelector);
                    }
                }

                //var element = FindElement.QuerySelectorAll(elementSelector, propSelector);
                //if (element.Count > 0)
                //    return FindElement.CheckElementContentProps(element, propSelector);

                logger.LogDebug("document:" + selector.ToString());
                foreach (var nodeSelector in propSelector)
                {
                    var tagName = nodeSelector.Name;
                    if (tagName == "tab")
                        continue;
                    if (nodeSelector.Props.Count == 0)
                    {
                        paths.Add(tagName);
                    }
                    foreach (var prop in nodeSelector.Props)
                    {
                        if (prop.Accurate.As<bool>())
                        {
                            if (prop.Name == "id")
                            {
                                var cssSelector = string.Empty;
                                if (paths.Count > 0)
                                    cssSelector = string.Join(">", paths) + $">{tagName}#{prop.Value}";
                                else
                                    cssSelector = tagName + "#" + prop.Value;
                                eleSelectors = ElementSelectorFindAll(listeleSelectors, cssSelector, tagName, prop);

                                if (eleSelectors.Count == 0)
                                    eleSelectors = ElementSelectorFindByID(listeleSelectors, prop);
                            }
                            else if (prop.Name == "class")
                            {
                                string cssSelector = String.Empty;
                                if (paths.Count > 0)
                                    cssSelector = string.Join(">", paths) + $">{tagName}";
                                if (string.IsNullOrEmpty(cssSelector))
                                    eleSelectors = ElementSelectorFindChildren(listeleSelectors, cssSelector, tagName, prop);
                                if (eleSelectors.Count == 0)
                                {
                                    eleSelectors = ElementSelectorFindAll(listeleSelectors, cssSelector, tagName, prop);
                                }
                                if (eleSelectors.Count == 0)
                                {
                                    cssSelector = string.Join(">", paths);
                                    eleSelectors = ElementSelectorFindChildren(listeleSelectors, cssSelector, tagName, prop);
                                }
                            }
                            else if (prop.Name == "@index")
                            {
                                if (!prop.Pattern.Equals("equal"))
                                {
                                    throw new IEException(eErrorMessageType.Error_NotFoundElement);
                                }
                                int.TryParse(prop.Value, out int nthChild);
                                foreach (var item in listeleSelectors)
                                {
                                    if (paths.Count == 0)
                                    {
                                        IHTMLElement queryElement = item as IHTMLElement;
                                        IHTMLElementCollection elementCollection = queryElement.children;
                                        for (int nthIndex = 0; nthIndex < elementCollection.length; nthIndex++)//nth-child
                                        {
                                            IHTMLElement e = elementCollection.item(nthIndex) as IHTMLElement;
                                            logger.LogDebug("@index element QuerySelector:" + (e == null).ToString());
                                            if (e != null && e.tagName.ToLower() == tagName && nthChild - 1 == nthIndex)
                                            {
                                                eleSelectors.Add(e as IElementSelector);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var cssSelector = string.Join(">", paths);
                                        logger.LogDebug("@index cssSelector:" + cssSelector);
                                        IHTMLDOMChildrenCollection elementCollection = item.querySelectorAll(cssSelector);
                                        for (int childIndex = 0; childIndex < elementCollection.length; childIndex++)
                                        {
                                            IHTMLElement ele = elementCollection.item(childIndex) as IHTMLElement;
                                            IHTMLElementCollection eleCollection = ele.children as IHTMLElementCollection;
                                            if (eleCollection.length > nthChild - 1)
                                            {
                                                for (int nthIndex = 0; nthIndex < eleCollection.length; nthIndex++)//nth-child
                                                {
                                                    IHTMLElement e = eleCollection.item(nthIndex) as IHTMLElement;
                                                    logger.LogDebug(tagName + " element QuerySelector:" + LogBlock.NullCheck(e, "e"));
                                                    logger.LogDebug("cssSelector: " + cssSelector);
                                                    if (e != null && e.tagName.ToLower() == tagName && nthChild - 1 == nthIndex)
                                                    {
                                                        eleSelectors.Add(e as IElementSelector);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            //else if (prop.Name == "expressionValue")
                            //{
                            //    continue;
                            //}
                            else
                            {
                                var cssSelector = string.Empty;
                                if (paths.Count > 0)
                                    cssSelector = string.Join(">", paths) + $">{tagName}[{prop.Name}='{prop.Value}']";
                                else
                                    cssSelector = $"{tagName}[{prop.Name}='{prop.Value}']";
                                eleSelectors = ElementSelectorFindAll(listeleSelectors, cssSelector, tagName, prop);
                                if (eleSelectors.Count == 0)
                                {
                                    cssSelector = string.Join(">", paths);
                                    eleSelectors = ElementSelectorFindChildren(listeleSelectors, cssSelector, tagName, prop);
                                }
                            }
                        }
                        paths = new List<string>();
                    }

                    if (nodeSelector.Props.Count != 0)
                    {
                        listeleSelectors = eleSelectors;
                        eleSelectors = new List<IElementSelector>();
                    }
                }

                if (listeleSelectors.Count == 0)
                {
                    listeleSelectors = new List<IElementSelector>();
                    listeleSelectors.Add(elementSelector);

                    foreach (var nodeSelector in propSelector)
                    {
                        var tagName = nodeSelector.Name;
                        if (tagName == "tab")
                            continue;
                        if (nodeSelector.Props.Count != 0)
                        {
                            foreach (var prop in nodeSelector.Props)
                            {
                                if (prop.Name == "@index")
                                {
                                    if (!prop.Pattern.Equals("equal"))
                                    {
                                        throw new IEException(eErrorMessageType.Error_NotFoundElement);
                                    }
                                    int.TryParse(prop.Value, out int nthChild);
                                    foreach (var item in listeleSelectors)
                                    {
                                        if (paths.Count == 0)
                                        {
                                            IHTMLElement queryElement = item as IHTMLElement;
                                            IHTMLElementCollection elementCollection = queryElement.children;
                                            for (int nthIndex = 0; nthIndex < elementCollection.length; nthIndex++)//nth-child
                                            {
                                                IHTMLElement e = elementCollection.item(nthIndex) as IHTMLElement;
                                                logger.LogDebug("@index element QuerySelector:" + (e == null).ToString());
                                                if (e != null && e.tagName.ToLower() == tagName && nthChild - 1 == nthIndex)
                                                {
                                                    eleSelectors.Add(e as IElementSelector);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var cssSelector = string.Join(">", paths);
                                            logger.LogDebug("@index cssSelector:" + cssSelector);
                                            IHTMLDOMChildrenCollection elementCollection = item.querySelectorAll(cssSelector);
                                            for (int childIndex = 0; childIndex < elementCollection.length; childIndex++)
                                            {
                                                IHTMLElement ele = elementCollection.item(childIndex) as IHTMLElement;
                                                IHTMLElementCollection eleCollection = ele.children as IHTMLElementCollection;
                                                if (eleCollection.length > nthChild - 1)
                                                {
                                                    for (int nthIndex = 0; nthIndex < eleCollection.length; nthIndex++)//nth-child
                                                    {
                                                        IHTMLElement e = eleCollection.item(nthIndex) as IHTMLElement;
                                                        logger.LogDebug(tagName + " element QuerySelector:" + LogBlock.NullCheck(e, "e"));
                                                        logger.LogDebug("cssSelector: " + cssSelector);
                                                        if (e != null && e.tagName.ToLower() == tagName && nthChild - 1 == nthIndex)
                                                        {
                                                            eleSelectors.Add(e as IElementSelector);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    eleSelectors = ElementSelectorFindChildren(listeleSelectors, "", tagName, prop);
                                }
                            }
                        }
                        else
                        {
                            eleSelectors = ElementSelectorFindChildren(listeleSelectors, "", tagName);
                        }

                        listeleSelectors = eleSelectors;
                        eleSelectors = new List<IElementSelector>();
                        if (listeleSelectors.Count == 0)
                            throw new IEException(eErrorMessageType.Error_NotFoundElement);
                    }
                }
                if (listeleSelectors.Count == 0)
                    throw new IEException(eErrorMessageType.Error_NotFoundElement);
                if (listeleSelectors.Count != 0)
                {
                    foreach (var item in listeleSelectors)
                    {
                        if (paths.Count != 0)
                        {
                            IHTMLDOMChildrenCollection childrenCollection = item.querySelectorAll(String.Join(">", paths));
                            for (int nthIndex = 0; nthIndex < childrenCollection.length; nthIndex++)//nth-child
                            {
                                IHTMLElement e = childrenCollection.item(nthIndex) as IHTMLElement;
                                elements.Add(e);
                            }
                        }
                        else
                        {
                            elements.Add(item as IHTMLElement);
                        }
                    }
                }
                else
                {
                    foreach (var item in listeleSelectors)
                    {
                        elements.Add(item as IHTMLElement);
                    }
                }
                return elements;
            }
            catch (Exception e)
            {
                logger.LogError($"FindIHTMLElement {e}");
                return new List<IHTMLElement>();
            }
        }


        public static IHTMLFrameBase2 GetChildFrameByIndex(HTMLDocument document, int index)
        {
            object pvarIndex = index;
            int i = 0;
            foreach (IHTMLFrameBase2 frameElement in document.getElementsByTagName("iframe"))
            {
                if (i == index)
                    return frameElement;
                i++;
            }
            i = 0;
            foreach (IHTMLFrameBase2 frameElement in document.getElementsByTagName("frame"))
            {

                if (i == index)
                    return frameElement;
                i++;
                //if (frameElement.contentWindow == document.parentWindow.frames.item(ref pvarIndex))
                //{
                //    return frameElement;
                //}
            }
            return null;
        }
        public static IHTMLElement FromUniqueId(string uniqueId, HTMLDocument document)
        {
            var tokens = uniqueId.Split('/');
            if (tokens.Length < 3)
            {
                throw new IEException();
            }
            var tagName = tokens[tokens.Length - 1];
            Array.Clear(tokens, tokens.Length - 1, 1);
            var value = string.Join("/", tokens);
            var uniqueValue = value.Remove(value.Length - 1);
            var uniqueArray = uniqueId.Split('/');
            IHTMLWindow2 w2 = WebWindowHelper.WebDocumentWindow(document);
            logger.LogInformation($"windows ElementFromUniqueId {LogBlock.NullCheck(w2, "w2")}");
            IHTMLElement ele = null;
            //if (_ieVersion > 8)
            ele = Utils.QuerySelector(w2, $"IIRPA.dom.querySelectorAll(\"{tagName}[{UNIQUE_KEY}='{uniqueValue}']\")[0]") as IHTMLElement;
            if (ele == null)
            {
                IHTMLDocument2 doc2 = DocumentHelper.DocumentFromWindow(w2);
                logger.LogInformation(LogBlock.NullCheck(doc2, "doc2"));
                IDocumentSelector elementSelector = doc2 as IDocumentSelector;
                logger.LogInformation($"elementSelector  is null {LogBlock.NullCheck(elementSelector, "elementSelector")}");
                IHTMLDOMChildrenCollection collection = null;
                var cssSelector = $"{tagName.ToLower()}[{UNIQUE_KEY}='{uniqueValue}']";
                collection = elementSelector.querySelectorAll($"{tagName}[{UNIQUE_KEY}='{uniqueValue}']");
                logger.LogInformation(LogBlock.NullCheck(collection, "collection"));
                for (int i = 0; i < collection.length; i++)
                {
                    logger.LogInformation("Find Element");
                    IHTMLElement child = collection.item(i) as IHTMLElement;
                    if (child != null)
                    {
                        ele = child;
                    }
                }
            }
            return ele;
        }
        public static IHTMLElement ElementFromUniqueId(string uniqueId, HTMLDocument document)
        {
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                var tokens = uniqueId.Split('/');
                if (tokens.Length < 3)
                {
                    throw new IEException();
                }
                var tagName = tokens[tokens.Length - 1];
                Array.Clear(tokens, tokens.Length - 1, 1);
                var value = string.Join("/", tokens);
                var uniqueValue = value.Remove(value.Length - 1);
                var uniqueArray = uniqueId.Split('/');
                IHTMLWindow2 w2 = WebWindowHelper.WebDocumentWindow(document);

                for (int i = 1; i < uniqueArray.Length - 2; i++)
                {
                    try
                    {
                        var frame = Utils.GetChildFrameByIndex(document, Convert.ToInt32(uniqueArray[i]));
                        if (frame != null)
                        {
                            w2 = WebWindowHelper.WebFrameWindow(frame);
                            document = w2.document as HTMLDocument;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                logger.LogInformation($"windows ElementFromUniqueId {LogBlock.NullCheck(w2, "w2")}");
                IHTMLElement ele = null;
                //if (_ieVersion > 8)
                ele = Utils.QuerySelector(w2, $"IIRPA.dom.querySelectorAll(\"{tagName}[{UNIQUE_KEY}='{uniqueValue}']\")[0]") as IHTMLElement;
                stopWatch.Stop();
                logger.LogInformation($"get uniqueid element time {stopWatch.ElapsedMilliseconds}");
                if (ele == null)
                {
                    IHTMLDocument2 doc2 = DocumentHelper.DocumentFromWindow(w2);
                    logger.LogInformation(LogBlock.NullCheck(doc2, "doc2"));
                    IDocumentSelector elementSelector = doc2 as IDocumentSelector;
                    logger.LogInformation(LogBlock.NullCheck(elementSelector, "elementSelector"));
                    IHTMLDOMChildrenCollection collection = null;
                    var cssSelector = $"{tagName.ToLower()}[{UNIQUE_KEY}='{uniqueValue}']";
                    collection = elementSelector.querySelectorAll($"{tagName}[{UNIQUE_KEY}='{uniqueValue}']");
                    logger.LogInformation(LogBlock.NullCheck(collection, "collection"));
                    for (int i = 0; i < collection.length; i++)
                    {
                        logger.LogInformation("Find Element");
                        IHTMLElement child = collection.item(i) as IHTMLElement;
                        if (child != null)
                        {
                            ele = child;
                        }
                    }
                }
                return ele;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "ElementFromUniqueId error");
                return null;
            }
        }
        public static string BuildCssSelector(IHTMLWindow2 win, string uniqueId, string frameIndex)
        {
            //if (_ieVersion > 8)
            {
                StringBuilder selector = new StringBuilder();
                logger.LogDebug($"BuildCssSelector {uniqueId}");
                try
                {
                    selector.Append(Utils.InvokeScript(win, "buildCssSelector", uniqueId, frameIndex));
                }
                catch (Exception e)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "BuildCssSelector Error");
                }
                return selector.ToString();
            }
            return string.Empty;
        }

        public static int GetNthChild(IHTMLElement element)
        {
            try
            {
                IHTMLUniqueName uniqueCurrentElement = element as IHTMLUniqueName;
                IHTMLElement parent = element.parentElement as IHTMLElement;
                if (parent == null)
                    return -1;
                IHTMLElementCollection coollection = parent.children;
                int catchnotelementcount = 0;
                for (int i = 0; i < coollection.length; i++)
                {
                    IHTMLElement e = coollection.item(i) as IHTMLElement;
                    IHTMLUniqueName uniqueElement = e as IHTMLUniqueName;
                    if (e is IHTMLCommentElement)
                    {
                        catchnotelementcount += 1;
                        continue;
                    }
                    if (e.Equals(element) || uniqueCurrentElement.uniqueID == uniqueElement.uniqueID)
                        return i - catchnotelementcount;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "GetNthChild Error");
            }
            return -1;
        }
        public static bool CheckNumber(string str)
        {
            if (!string.IsNullOrEmpty(str))
                return Regex.Matches(str, "[0-9]").Count > 0;
            return false;
        }
        private static int GetChildCount(IHTMLElement element)
        {
            IHTMLDOMChildrenCollection elementCollection = element.children;
            if (elementCollection == null || elementCollection.length == 0)
                return -1;
            return elementCollection.length;
        }
        public static List<Props> GetNodeOPtionProps(IHTMLElement element, bool isNeednthchild)
        {
            List<Props> props = new List<Props>();
            Props prop = null;
            try
            {
                try
                {
                    if (!string.IsNullOrEmpty(element.getAttribute("id") as string))
                    {
                        prop = new Props();
                        prop.Name = "id";
                        prop.Value = element.getAttribute("id");
                        prop.Pattern = "equal";
                        prop.Accurate = "false";
                        props.Add(prop);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"get id error:{ex}");
                }

                try
                {
                    if (!string.IsNullOrEmpty(element.className))
                    {
                        prop = new Props();
                        prop.Name = "class";
                        prop.Value = element.className;
                        prop.Pattern = "contain";
                        prop.Accurate = "false";
                        props.Add(prop);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"get classname error:{ex}");
                }
                try
                {

                    if (GetChildCount(element) == -1)
                    {
                        if (!string.IsNullOrEmpty(element.innerText) && element.innerText.Length < 20)
                        {
                            prop = new Props();
                            prop.Name = "innertext";
                            prop.Value = element.innerText;
                            prop.Pattern = "equal";
                            prop.Accurate = "false";
                            props.Add(prop);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"get innertext error:{ex}");
                }

                if (!string.IsNullOrEmpty(element.getAttribute("src") as string))
                {
                    try
                    {
                        prop = new Props();
                        prop.Name = "src";
                        var tags = new string[] { "iframe", "frame" };
                        if (tags.Contains(element.tagName.ToLower()))
                        {
                            IHTMLIFrameElement3 frameelement = element as IHTMLIFrameElement3;
                            if (frameelement != null)
                                prop.Value = frameelement.src;
                            else
                                prop.Value = element.getAttribute("src");
                        }
                        prop.Pattern = "contain";
                        prop.Accurate = "false";
                        props.Add(prop);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"get frame src error:{ex}");
                    }
                }
                var index = GetNthChild(element) + 1;
                if (index > 1)
                {
                    prop = new Props();
                    prop.Name = "@index";
                    prop.Value = index.ToString();
                    prop.Pattern = "equal";
                    prop.Accurate = "false";
                    props.Add(prop);

                }
                try
                {
                    if (!string.IsNullOrEmpty(element.getAttribute("type") as string))
                    {
                        prop = new Props();
                        prop.Name = "type";
                        prop.Value = element.getAttribute("type");
                        prop.Pattern = "equal";
                        prop.Accurate = "false";
                        props.Add(prop);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"get type error:{ex}");
                }
                try
                {
                    if (!string.IsNullOrEmpty(element.getAttribute("name") as string))
                    {
                        prop = new Props();
                        prop.Name = "name";
                        prop.Value = element.getAttribute("name");
                        prop.Pattern = "equal";
                        prop.Accurate = "false";
                        props.Add(prop);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"get name error:{ex}");
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "GetNodeOPtionProps Error");
            }
            return props;
        }

        public static string IHTMLBuildSelector(IHTMLWindow2 win, string uniqueId, int frameIndex = -1)
        {
            IHTMLDocument2 doc2 = DocumentHelper.DocumentFromWindow(win);
            IHTMLDocument7 doc7 = doc2 as IHTMLDocument7;
            logger.LogInformation(LogBlock.NullCheck(doc2, "IHTMLBuildSelector doc2"));
            IHTMLElement element = null;
            if (!string.IsNullOrEmpty(uniqueId))
                element = FromUniqueId(uniqueId, doc2 as HTMLDocument);
            else if (frameIndex > -1)
                element = GetChildFrameByIndex(doc2 as HTMLDocument, frameIndex) as IHTMLElement;
            else
                logger.LogInformation($"uniqueId: {uniqueId} frameIndex:{frameIndex}");

            logger.LogDebug(LogBlock.NullCheck(element, "IHTMLBuildSelector element"));
            IHTMLElement contextNode = element;
            bool findUniqueId = false;
            List<SelectorProps> selectorProps = new List<SelectorProps>();
            SelectorProps selectorProp = null;
            while (!findUniqueId && contextNode != null)
            {
                findUniqueId = false;
                string nodeName = contextNode.tagName.ToLower();
                if (nodeName == "body" || nodeName == "html")
                    break;
                selectorProp = new SelectorProps();
                selectorProp.Name = nodeName;
                selectorProp.Props = GetNodeOPtionProps(contextNode, false);
                selectorProp.Type = "web";
                foreach (var item in selectorProp.Props)
                {
                    int classLength = 0;
                    if (item.Name == "class")
                    {
                        try
                        {
                            IHTMLElementCollection eleCollection = doc7.getElementsByClassName(item.Value);
                            if (eleCollection != null)
                                classLength = eleCollection.length;
                        }
                        catch
                        {
                        }
                    }
                    if (!findUniqueId && item.Name == "id" && !CheckNumber(item.Value))
                    {
                        item.Accurate = "true";
                        IElementSelector elementSelector = doc2.body as IElementSelector;
                        if (elementSelector.querySelectorAll($"#{item.Value}").length == 1)
                            findUniqueId = true;
                        break;
                    }
                    //else if (item.Name == "class" && classLength == 1)
                    //{
                    //    item.Accurate = "true";
                    //    break;
                    //}
                    else if (item.Name == "type")
                    {
                        item.Accurate = "true";
                        break;
                    }
                    else if (item.Name == "src")
                    {
                        item.Accurate = "true";
                        break;
                    }
                    else if (item.Name == "innertext")
                    {
                        item.Accurate = "true";
                        break;
                    }
                    else if (item.Name == "@index")
                    {
                        item.Accurate = "true";
                        break;
                    }
                }
                contextNode = contextNode.parentElement;
                logger.LogDebug("IHTMLBuildSelector  contextNode");
                selectorProps.Add(selectorProp);
            }
            for (int i = 0; i < selectorProps.Count; i++)
            {
                if (i == 0)
                {
                    if (selectorProps[i].Props.Count > 0)
                    {
                        bool isInnerText = false;
                        foreach (var item in selectorProps[i].Props)
                        {
                            if (findUniqueId)
                            {
                                break;
                            }
                            if (item.Name == "innertext")
                            {
                                isInnerText = true;
                                item.Accurate = "true";
                            }
                            else if (item.Name != "innertext" && item.Accurate == "true" && isInnerText)
                                item.Accurate = "false";
                        }
                        continue;
                    }
                }
                else if (i == selectorProps.Count - 1)
                {
                    foreach (var item in selectorProps[i].Props)
                    {
                        if (item.Name == "id" && item.Accurate.As<bool>())
                            break;
                        else if (item.Name == "@index" && !item.Accurate.As<bool>())
                            item.Accurate = "true";
                        else if (item.Name == "class" && !item.Accurate.As<bool>())
                            item.Accurate = "true";

                        if (item.Name == "innertext")
                            item.Accurate = "false";
                    }
                }
                else
                {
                    foreach (var item in selectorProps[i].Props)
                    {
                        if (item.Name == "innertext")
                            item.Accurate = "false";
                        if (item.Name == "class" && !item.Accurate.As<bool>())
                        {
                            item.Accurate = "false";
                        }
                        else if (item.Name == "@index" && !item.Accurate.As<bool>())
                        {
                            item.Accurate = "true";
                        }
                    }
                }
            }
            if (selectorProps.Count > 1)
                selectorProps.Reverse();
            return JsonConvert.SerializeObject(selectorProps);
        }
        public static bool ValidateJSON(string input)
        {
            input = input.Trim();
            if ((input.StartsWith("{") && input.EndsWith("}")) || //For object
                (input.StartsWith("[") && input.EndsWith("]"))) //For array 
            {
                try
                {
                    var jarray = JArray.Parse(input);
                }
                catch (JsonReaderException ex)
                {
                    logger.LogError(ex);
                    return false;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }
        public static string BuildSelector(
            IHTMLWindow2 win,
            string uniqueId,
            string frameIndex,
            bool innerTextFirst = false,
            bool rawData = false,
            bool excludeInnerText = false,
            bool excludeHiddenEle = true,
            List<Dictionary<string, object>> validNodeAttributes = null
            )
        {
            StringBuilder selector = new StringBuilder();
            logger.LogDebug($"BuildSelector uniqueid {uniqueId} or frameindex {frameIndex}");
            try
            {
                string nodeSelector = string.Empty;
                try
                {
                    List<string> _params = new List<string>() {
                        uniqueId,
                        frameIndex,
                        innerTextFirst.ToString().ToLower(),
                        rawData.ToString().ToLower(),
                        excludeInnerText.ToString().ToLower(),
                        excludeHiddenEle.ToString().ToLower()
                    };
                    if (validNodeAttributes != null)
                    {
                        _params.Add(JsonConvert.SerializeObject(validNodeAttributes));
                    }
                    var resultSelector = Utils.InvokeScript(win,
                            "buildSelector",
                            _params.ToArray()
                            );
                    if (!string.IsNullOrEmpty(resultSelector))
                    {
                        nodeSelector = Regex.Unescape(resultSelector);
                        nodeSelector = nodeSelector.Trim('\"');
                        logger.LogDebug("Selector:" + nodeSelector);
                    }
                    if (string.IsNullOrEmpty(nodeSelector) || !ValidateJSON(nodeSelector))
                    {
                        logger.LogError($"Again BuildSelector uniqueid {uniqueId} or frameindex {frameIndex}");
                        if (!string.IsNullOrEmpty(uniqueId))
                            nodeSelector = IHTMLBuildSelector(win, uniqueId, -1);
                        else
                        {
                            int.TryParse(frameIndex, out int value);
                            nodeSelector = IHTMLBuildSelector(win, "", value);
                        }
                    }
                }
                catch (Exception e)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "JSON Error");
                }
                selector.Append(nodeSelector);
                return selector.ToString();
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "BuildSelector Error");
            }
            return null;
        }
        public static IHTMLElement getAndScrollLastElement(IHTMLWindow2 window, params string[] args)
        {
            return run.InvokeScript(window, "getAndScrollLastElement", false, args) as IHTMLElement;
        }
        public static IHTMLElement ScrollIntoView(IHTMLWindow2 window, string uniqueId, string location = "center", int elementLeft = 0, int elementTop = 0)
        {
            return run.InvokeScript(window, "ScrollIntoView", true, uniqueId, location, elementLeft, elementTop) as IHTMLElement;
        }
        public static IHTMLElement ScrollIntoView_V2(IHTMLWindow2 window, string uniqueId)
        {
            return run.InvokeScript(window, "ScrollIntoView_V2", true, uniqueId) as IHTMLElement;
        }
        public static void scrollTo(IHTMLWindow2 window, string paramsStr)
        {
            run.InvokeScript(window, "scrollTo", false, @"""", paramsStr);
        }
        public static IHTMLElement GetElementByIndex(IHTMLWindow2 window, int index)
        {
            return run.InvokeScript(window, "GetElement", false, index) as IHTMLElement;
        }
        public static object QuerySelector(IHTMLWindow2 window, string method)
        {
            return run.InvokeScript(window, method, false);
        }
        public static object QuerySelector(IHTMLWindow2 window, object selector, string version, string cssSelector = "", string parentId = "", bool discernHidden = true)
        {
            logger.LogInformation("windows QuerySelector:" + selector.ToString());
            if (string.IsNullOrEmpty(version))
                version = "''";
            if (string.IsNullOrEmpty(cssSelector))
                cssSelector = "''";
            else
                cssSelector = $"'{cssSelector}'";
            if (string.IsNullOrEmpty(parentId))
                parentId = "''";
            else
                parentId = $"'{parentId}'";
            return run.InvokeScript(window, "QuerySelector", false, selector, cssSelector, version, parentId, discernHidden.ToString().ToLower());
        }
        public static object QueryElementsAll(IHTMLWindow2 window, string frameTree, string tagNames, string attrKeywords)
        {
            if (string.IsNullOrEmpty(tagNames))
                tagNames = "''";
            if (string.IsNullOrEmpty(attrKeywords))
                attrKeywords = "''";
            return run.InvokeScript(window, "IIRPA.dom.getElements", false, frameTree, tagNames, attrKeywords);
        }

        public static object queryCollectionDataV2(IHTMLWindow2 window, params string[] args)
        {
            return run.InvokeScript(window, "queryCollectionDataV2", false, args);
        }
        public static object getTurningPageElement(IHTMLWindow2 window, params string[] args)
        {
            return run.InvokeScript(window, "getTurningPageElement", false, args);
        }
        public static object InvokeScriptObject(IHTMLWindow2 window, string scritp, params string[] args)
        {
            return run.InvokeScript(window, scritp, true, args);
        }
        public static string InvokeScript(IHTMLWindow2 window, string scritp, params string[] args)
        {
            return run.InvokeScript(window, scritp, true, args)?.ToString();
        }
        public static string SetUniqueId(IHTMLWindow2 window, List<string> frameTree, IHTMLElement element)
        {
            if (run.InvokeScript(window, "window._iirpa_recent_id") == null)
            {
                run.InvokeScript(window, "window._iirpa_recent_id=1");
            }
            string uniqueValue = element.getAttribute(Utils.UNIQUE_KEY, 2) as string;
            if (uniqueValue != null)
            {
                var tokens = uniqueValue.Split('/');
                string[] frame = frameTree.ToArray();
                if (frame.Equals(tokens))
                {
                    uniqueValue = null;
                }
            }
            if (uniqueValue == null)
            {
                run.InvokeScript(window, "window._iirpa_recent_id += 1");
                uniqueValue = $"{string.Join("/", frameTree)}/{run.InvokeScript(window, "window._iirpa_recent_id")}";
                element.setAttribute(Utils.UNIQUE_KEY, uniqueValue);
            }
            return $"{uniqueValue}/{element.tagName}";
        }
        public static int GetFrameIndex(IHTMLWindow2 win)
        {
            if (win.parent == win || win.parent == null)
            {
                return -1;
            }
            else
            {
                IHTMLWindow2 win_2 = WebWindowHelper.WebWindow(win);
                IHTMLWindow2 win2 = WebWindowHelper.WebParentWindow(win_2.document as HTMLDocument);
                var cons = win2.frames;
                for (int i = 0; i < cons.length; i++)
                {
                    IHTMLWindow2 w = cons.item(i) as IHTMLWindow2;
                    if (w == win)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }
        public static int offsetX = 0;
        public static int offsetY = 0;
        public static IHTMLWindow2 currentWindow = null;
        public static void pointClientToPage(ref Point point, double zoom)
        {
            point.X = Convert.ToInt32(point.X / zoom);
            point.Y = Convert.ToInt32(point.Y / zoom);
        }
        public static double ZoomLevel(IHTMLWindow2 win2)
        {
            var zoomStr = Utils.InvokeScript(win2, "IIRPA.zoomLevel()");
            if (zoomStr != null)
                logger.LogDebug($"zoom {zoomStr}");
            return string.IsNullOrEmpty(zoomStr) ? 1 : double.Parse(zoomStr);
        }
        public static IHTMLElement ElementFromPoint(HTMLDocument document, Point point, List<string> frameList, ref IHTMLWindow2 win2)
        {
            try
            {
                point.X -= location.X;
                point.Y -= location.Y;
                var zoom = ZoomLevel(win2);
                pointClientToPage(ref point, zoom);
                IHTMLElement ele = null;
                ele = document.elementFromPoint(point.X, point.Y);
                var ele2 = ele as IHTMLElement2;
                var rect = ele2.getBoundingClientRect();
                var left = ele2.currentStyle.paddingLeft;
                var top = ele2.currentStyle.paddingTop;
                double.TryParse(left?.Replace("px", ""), out double panddingleft);
                double.TryParse(top?.Replace("px", ""), out double panddingtop);
                offsetX = 0;
                offsetY = 0;
                while (true)
                {
                    if (IsFrameElement(ele))
                    {
                        IHTMLElement2 frameElement = ele as IHTMLElement2;
                        var frameRect = frameElement.getBoundingClientRect();
                        left = frameElement.currentStyle.paddingLeft;
                        top = frameElement.currentStyle.paddingTop;
                        double.TryParse(left?.Replace("px", ""), out double pleft);
                        double.TryParse(top?.Replace("px", ""), out double ptop);
                        panddingleft += pleft;
                        panddingtop += ptop;
                        point.X -= (frameRect.left + (int)panddingleft);
                        point.Y -= (frameRect.top + (int)panddingtop);
                        offsetX += frameRect.left;
                        offsetY += frameRect.top;

                        IHTMLFrameBase2 base2 = ele as IHTMLFrameBase2;
                        win2 = WebWindowHelper.WebFrameWindow(base2);
                        Utils.InjectJavaScript(win2);
                        var doc = win2.document as HTMLDocument;
                        var frameId = Utils.GetFrameIndex(win2);
                        if (frameId >= 0)
                            frameList.Add(frameId.ToString());
                        ele = doc.elementFromPoint((int)(point.X), (int)(point.Y));
                    }
                    else
                    {
                        break;
                    }
                }
                return ele;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "ElementFromPoint Error");
                return null;
            }
        }
        public static bool IsFrameElement(object o)
        {
            try
            {
                if (o != null)
                {
                    var element = o as IHTMLElement;
                    var tagName = element.tagName.ToLower();
                    return tagName == "frame" || tagName == "iframe";
                }
                return false;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IsFrameElement Error");
                return false;
            }
        }

        private static Rectangle location = new Rectangle();

        public static Rectangle GetElementBounding(IHTMLElement element, double zoom = 1.00)
        {
            var ele = element as IHTMLElement2;
            if (ele != null)
            {
                var rect = ele.getBoundingClientRect();
                int panddingleft = 0;
                int panddingtop = 0;
                logger.LogDebug($"offsetX:{offsetX} offsetY:{offsetY}");
                if (offsetX == 0 || offsetY == 0)
                {
                    if (IsFrameElement(element))
                    {
                        IHTMLFrameBase2 frame = ele as IHTMLFrameBase2;
                        IHTMLElement2 frameElement = frame as IHTMLElement2;
                        var frameRect = frameElement.getBoundingClientRect();
                        logger.LogDebug($"element bounding left:{frameRect.left} top:{frameRect.top}");
                        panddingleft += frameRect.left;
                        panddingtop += frameRect.top;
                    }
                }
                return new Rectangle((int)((rect.left + offsetX) * zoom + location.X), (int)((rect.top + offsetY) * zoom + location.Y), (int)((rect.right - rect.left) * zoom), (int)((rect.bottom - rect.top) * zoom));
            }
            return new Rectangle();
        }
        /// <summary>
        /// 批量返回涉及到元素的uniqueid
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="cssSelector"></param>
        /// <param name="win"></param>
        /// <param name="index"></param>
        /// <returns></returns>

        public static object SetUniqueIdArray(IHTMLWindow2 window, string[] frameArry, int index = -1)
        {
            return run.InvokeScript(window, "SetUniqueIdArray", true, string.Join("/", frameArry), index);
        }
        public static List<string> BuildBatchSelector(IHTMLWindow2 win, string uniqueIds, string frameIndex, string attributeName)
        {
            List<string> batchSelector = new List<string>();

            logger.LogDebug($"BuildSelector {uniqueIds}");
            try
            {

                var nodeSelector = Utils.InvokeScript(win, "buildBatchSelector", uniqueIds, frameIndex, attributeName);
                logger.LogDebug("Selector:" + nodeSelector);

                var nodes = nodeSelector.Split('&');
                foreach (var node in nodes)
                {
                    StringBuilder selector = new StringBuilder();
                    try
                    {
                        JArray.Parse(node);
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Again BuildSelector" + uniqueIds);
                        //if (!string.IsNullOrEmpty(uniqueId))
                        //    nodeSelector = IHTMLBuildSelector(win, uniqueId, -1);
                        //else
                        //{
                        //    int.TryParse(frameIndex, out int value);
                        //    nodeSelector = IHTMLBuildSelector(win, "", value);
                        //}
                    }
                    selector.Append(node);
                    batchSelector.Add(selector.ToString());
                }
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "BuildSelector Error:");
            }
            return batchSelector;
        }
    }

}

