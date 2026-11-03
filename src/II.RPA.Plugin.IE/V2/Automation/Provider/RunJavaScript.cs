using II.RPA.WinAutomation.CommonUtility;
using MSHTML;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Logging.Formatting;

namespace II.RPA.Plugin.IE.V2.Automation.Provider
{
    internal class RunJavaScript
    {
        private static ILogger logger = Logger.OfContext();
        private static readonly object _locker = new object();
        public string execScript(IHTMLWindow2 window, string javaScript)
        {
            lock (_locker)
            {
                try
                {
                    return window.execScript(javaScript, language: "javascript");
                }
                catch (Exception e)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, $"execScript error");
                    return null;
                }
            }
        }
        public object InvokeScript(IHTMLWindow2 window, string method, bool isEscape = true, params object[] args)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                string @params = string.Empty;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args.Length - 1 == i || args.Length == 1)
                    {
                        var paramType = args[i].GetType();
                        if (paramType.Name != "String")
                        {
                            if (isEscape)
                                @params += $"\'{args[i]}\'";
                            else
                                @params += $"{args[i]}";
                        }
                        else
                        {
                            if (isEscape)
                                @params += $"\'{args[i]}\'";
                            else
                                @params += $"{args[i]}";
                        }
                    }
                    else
                    {
                        if (isEscape)
                            @params += $"\'{args[i]}\',";
                        else
                            @params += $"{args[i]},";
                    }
                }
                string jsMethod = string.Empty;
                if (args.Length != 0)
                    jsMethod = $"{method}({@params})";
                else
                    jsMethod = $"{method}";
                Task<object> InvokeTask = Task<object>.Factory.StartNew(() =>
                {
                    try
                    {
                        lock (_locker)
                        {
                            return InvokeJavaScript(window, "eval", jsMethod);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex.Message, $"InvokeScript Task Method Warning");
                        return null;
                    }
                }
                );
                InvokeTask.Wait(1000);
                var result = InvokeTask.Result;
                stopWatch.Stop();
                logger.LogInformation($"InvokeScript running time:{stopWatch.ElapsedMilliseconds}");
                return result;
            }
            catch (Exception e)
            {
                stopWatch.Stop();
                logger.LogWarning($"InvokeScript running time {stopWatch.ElapsedMilliseconds} s");
                logger.LogWarning($"InvokeScript error {e.Message}");
                return null;
            }
        }
        private object InvokeJavaScript(object callee, string method, params object[] args)
        {
            Thread.Sleep(10);
            return callee.GetType().InvokeMember(method,
                BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public,
                null, callee, args);
        }
    }
}

