//using System.IO;
//using System.Text;
//using System.Reflection;
//using II.RPA.WinAutomation.CommonUtility;
//using System;
//using II.RPA.Plugin.Core;
//using II.RPA.Logging;
//using Microsoft.Extensions.Logging;


//namespace II.RPA.Plugin.IE.V2.Automation.Provider
//{
//    internal class IEExtensionProvider
//    {
//        public string CodeId { get; private set; }

//        public string Backgroud { get; private set; }

//        public string Content { get; private set; }

//        public string Sizzle { get; private set; }

//        public string Json2 { get; private set; }

//        private static ILogger logger = Logger.OfContext();

//        public IEExtensionProvider()
//        {
//            CodeId = "AI-indeedRPA";
//            //asm = System.Reflection.Assembly.Load("II.RPA.WebBrowser.Common");
//            asm = InstanceManager.GetAssembly("II.RPA.WebBrowser.Common");
//            try
//            {
//                if (asm == null)
//                {
//                    asm = System.Reflection.Assembly.Load("II.RPA.WebBrowser.Common");
//                }
//            }
//            catch (Exception ex)
//            {
//                logger.LogInformation($"加载公共类库WebBrowserCom {ex.ToString()}");
//            }
//            Content = LoadContentCode();
//            Sizzle = LoadContentSizzle();
//            Json2 = LoadContentJson2();
//        }
//        public static IEExtensionProvider Load()
//        {
//            return new IEExtensionProvider();
//        }
//        Assembly asm = null;//文件所在的项目 
//                            //private string LoadBackgroudCode()
//                            //{
//                            //    string[] strArray = { "backgroundscript.js" };
//                            //    StringBuilder stringBuilder = new StringBuilder();
//                            //    Stream sm = asm.GetManifestResourceStream($"II.RPA.Core.ExtensionScripts.{strArray[0]}");//文件的路径,程序集.路径.文件名 
//                            //    using (StreamReader sr = new StreamReader(sm))
//                            //    {
//                            //        //读取
//                            //        stringBuilder.Append(sr.ReadToEnd());
//                            //    }
//                            //    return stringBuilder.ToString();
//                            //}
//        private string LoadContentJson2()
//        {
//            string[] strArray = { "json2.js" };
//            StringBuilder stringBuilder = new StringBuilder();
//            try
//            {
//                foreach (var item in strArray)
//                {
//                    using (Stream sm = asm.GetManifestResourceStream($"II.RPA.WebBrowser.Common.ExtensionScripts.{item}"))//文件的路径,程序集.路径.文件名 
//                    {
//                        StreamReader sr = new StreamReader(sm);
//                        //读取
//                        stringBuilder.Append(sr.ReadToEnd());
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE Exception");
//            }
//            return stringBuilder.ToString();
//        }
//        private string LoadContentSizzle()
//        {
//            string[] strArray = { "sizzle.js", "json2.js" };
//            StringBuilder stringBuilder = new StringBuilder();
//            try
//            {
//                foreach (var item in strArray)
//                {
//                    using (Stream sm = asm.GetManifestResourceStream($"II.RPA.WebBrowser.Common.ExtensionScripts.{item}"))//文件的路径,程序集.路径.文件名 
//                    {
//                        StreamReader sr = new StreamReader(sm);
//                        //读取
//                        stringBuilder.Append(sr.ReadToEnd());
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE Exception");
//            }
//            return stringBuilder.ToString();
//        }
//        private string LoadContentCode()
//        {
//            string[] strArray = { "commonscript.js", "dataCollectionScript.js", "utils.js" };
//            StringBuilder stringBuilder = new StringBuilder();
//            try
//            {
//                foreach (var item in strArray)
//                {
//                    using (Stream sm = asm.GetManifestResourceStream($"II.RPA.WebBrowser.Common.ExtensionScripts.{item}"))//文件的路径,程序集.路径.文件名 
//                    {
//                        StreamReader sr = new StreamReader(sm);
//                        //读取
//                        stringBuilder.Append(sr.ReadToEnd());
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE Exception");
//            }
//            return stringBuilder.ToString();
//        }
//    }
//}

