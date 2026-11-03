using II.RPA.Logging;
using II.RPA.Plugin.IE.V2.Automation.Core;
using Microsoft.Extensions.Logging;
using MSHTML;
using System;
using System.Diagnostics;
using System.Threading;

namespace II.RPA.Plugin.IE.V2.Automation.Provider
{
    public class WebWindowHelper
    {
        private static ILogger logger = Logger.OfContext();

        public static IHTMLWindow2 WebFrameWindow(IHTMLFrameBase2 base2)
        {
            try
            {
                IHTMLWindow2 win2 = base2.contentWindow;
                Guid IID_IHTMLWindow2 = typeof(IHTMLWindow2).GUID;
                IServiceProvider pro = win2 as IServiceProvider;
                object objWin2 = null;
                pro.QueryService(ref IID_IHTMLWindow2, ref IID_IHTMLWindow2, out objWin2);
                return objWin2 as IHTMLWindow2;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE WebFrameWindow Error");
                return null;
            }
        }
        public static IHTMLWindow2 WebWindow(IHTMLWindow2 win2)
        {
            try
            {
                Guid IID_IHTMLWindow2 = typeof(IHTMLWindow2).GUID;
                IServiceProvider pro = win2 as IServiceProvider;
                object objWin2 = null;
                pro.QueryService(ref IID_IHTMLWindow2, ref IID_IHTMLWindow2, out objWin2);
                return objWin2 as IHTMLWindow2;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE WebWindow Error");
                return null;
            }
        }
        public static IHTMLWindow2 WebDocumentWindow(HTMLDocument document)
        {
            try
            {
                logger.LogDebug("WebDocumentWindow" + Thread.CurrentThread.ManagedThreadId.ToString());
                IHTMLWindow2 win2 = document.parentWindow;
                Guid IID_IHTMLWindow2 = typeof(IHTMLWindow2).GUID;
                IServiceProvider pro = win2 as IServiceProvider;
                object objWin2 = null;
                pro.QueryService(ref IID_IHTMLWindow2, ref IID_IHTMLWindow2, out objWin2);
                return objWin2 as IHTMLWindow2;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE WebDocumentWindow Error");
                return null;
            }

        }
        public static IHTMLWindow2 WebParentWindow(HTMLDocument document)
        {
            try
            {
                logger.LogDebug("WebParentWindow" + Thread.CurrentThread.ManagedThreadId.ToString());
                IHTMLWindow2 win2 = document.parentWindow;
                Guid IID_IHTMLWindow2 = typeof(IHTMLWindow2).GUID;
                IServiceProvider pro = win2 as IServiceProvider;
                object objWin2 = null;
                pro.QueryService(ref IID_IHTMLWindow2, ref IID_IHTMLWindow2, out objWin2);
                var win = objWin2 as IHTMLWindow2;
                return win.parent;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE WebParentWindow Error");
                return null;
            }
        }
        public static IHTMLWindow2 WebElementWindow(string uniqueId, HTMLDocument document)
        {
            try
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
                return w2;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE WebFrameWindow Error");
                return null;
            }
        }
    }
}

