using System.IO;
using System.Text;
using System.Reflection;
using II.RPA.WinAutomation.CommonUtility;
using System;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;
using System.Security.Cryptography;

namespace II.RPA.WebBrowser.Common.Basic
{
    public class ExtensionProvider
    {
        private static string codeID = null;
        private const string SCRIPT_CODEID_PLACEHOLDER_STR = "$@{ExtensionProvider_CODE_ID}";
        private ILogger logger = Logger.OfContext();

        private readonly static Lazy<ExtensionProvider> _instance = new Lazy<ExtensionProvider>(() => new ExtensionProvider());
        public static ExtensionProvider Instance => _instance.Value;

        Assembly asm = null;//文件所在的项目 
        private string backgroud = null;
        private string contentNotIE = null;
        private string contentCrawl = null;
        private string contentIE = null;
        private string sizzle = null;
        private string json2 = null;

        public string CodeId
        {
            get
            {
                return codeID;
            }
        }

        //public string Backgroud => new Lazy<string>(() => LoadBackgroudCode()).Value;
        public string Backgroud { get => backgroud ?? (backgroud = LoadBackgroudCode()); private set => backgroud = value; }

        //public string ContentNotIE => new Lazy<string>(() => LoadContentCode()).Value;
        public string ContentNotIE { get => contentNotIE ?? (contentNotIE = LoadContentCode()); private set => contentNotIE = value; }

        public string ContentCrawl { get => contentCrawl ?? (contentCrawl = LoadContentCrawlCode()); private set => contentCrawl = value; }

        public string ContentIE { get => contentIE ?? (contentIE = LoadContentIECode()); private set => contentIE = value; }

        public string Sizzle { get => sizzle ?? (sizzle = LoadContentSizzle()); private set => sizzle = value; }

        public string Json2 { get => json2 ?? (json2 = LoadContentJson2()); private set => json2 = value; }

        public string FilePath { get; private set; }

        private ExtensionProvider()
        {
            asm = typeof(ExtensionProvider).Assembly;
            codeID = GetCodeID(asm);
            logger.LogDebug($"ExtensionProvider codeID:{codeID}");
        }
        private string GetCodeID(Assembly assembly)
        {
            try
            {
                using (MD5 md5serv = MD5CryptoServiceProvider.Create())
                {
                    using (FileStream fileStream = new FileStream(assembly.Location, FileMode.Open, FileAccess.Read))
                    {
                        return EncryptUtils.MD5Encrypt(fileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"获取脚本CODEID发生异常，将使用版本号对应的固定CODEID");
                return EncryptUtils.MD5Encrypt($"II.RPA_{assembly.GetName().Version}");
            }
        }
        public ExtensionProvider(string file)
        {
            FilePath = file;
        }

        private string LoadBackgroudCode()
        {
            if (asm == null)
            {
                asm = GetType().Assembly;
            }
            StringBuilder stringBuilder = new StringBuilder();
            #region [测试代码]
            try
            {
                //string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                //string jsFileName = Path.Combine(assemblyFolder, @"ExtensionScripts\CrawlScript\Crawlbackground.js");
                //using (StreamReader sr = new StreamReader(jsFileName))
                //{
                //    //读取
                //    stringBuilder.Append(sr.ReadToEnd());
                //}
            }
            catch (Exception)
            {
            }
            #endregion

            #region [正式运行]
            try
            {
                string[] strArray = { "backgroundscript.js", "CrawlScript.Crawlbackground.js" };
                //string[] strArray = { "backgroundscript.js" };
                foreach (var item in strArray)
                {
                    using (Stream sm = asm.GetManifestResourceStream($"II.RPA.WebBrowser.Common.ExtensionScripts.{item}"))//文件的路径,程序集.路径.文件名 
                    {
                        using (StreamReader sr = new StreamReader(sm))
                        {
                            //读取
                            stringBuilder.Append(sr.ReadToEnd().Replace(ExtensionProvider.SCRIPT_CODEID_PLACEHOLDER_STR, this.CodeId));
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            #endregion
            return stringBuilder.ToString();
        }
        private string LoadContentCrawlCode()
        {
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                #region [测试代码]
                try
                {
                    //string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    //string jsFileName = Path.Combine(assemblyFolder, @"ExtensionScripts\CrawlScript\Crawlcontent.js");
                    //using (StreamReader sr = new StreamReader(jsFileName))
                    //{
                    //    //读取
                    //    stringBuilder.Append(sr.ReadToEnd());
                    //}
                }
                catch (Exception)
                {
                }
                #endregion

                #region [正式运行]
                try
                {
                    string[] strArray = { "CrawlScript.Crawlcontent.js" };
                    foreach (var item in strArray)
                    {
                        using (Stream sm = asm.GetManifestResourceStream($"II.RPA.WebBrowser.Common.ExtensionScripts.{item}"))//文件的路径,程序集.路径.文件名 
                        using (StreamReader sr = new StreamReader(sm))
                        {
                            //读取
                            stringBuilder.Append(sr.ReadToEnd().Replace(ExtensionProvider.SCRIPT_CODEID_PLACEHOLDER_STR, this.CodeId));
                        }
                    }
                }
                catch (Exception)
                {
                }
                #endregion
            }
            catch (RPAException e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "Chrome Exception");
            }
            return stringBuilder.ToString();
        }
        private string LoadContentCode()
        {
            if (asm == null)
            {
                asm = GetType().Assembly;
            }
            string[] strArray = { "commonscript.js", "dataCollectionScript.js", "contentscript.js", "webselect.js" };
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                foreach (var item in strArray)
                {
                    using (Stream sm = asm.GetManifestResourceStream($"II.RPA.WebBrowser.Common.ExtensionScripts.{item}"))//文件的路径,程序集.路径.文件名 
                    using (StreamReader sr = new StreamReader(sm))
                    {
                        //读取
                        stringBuilder.Append(sr.ReadToEnd().Replace(ExtensionProvider.SCRIPT_CODEID_PLACEHOLDER_STR, this.CodeId));
                    }

                }
            }
            catch (RPAException e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "Chrome Exception");
            }
            return stringBuilder.ToString();
        }

        private string LoadContentJson2()
        {
            string[] strArray = { "json2.js" };
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                foreach (var item in strArray)
                {
                    using (Stream sm = asm.GetManifestResourceStream($"II.RPA.WebBrowser.Common.ExtensionScripts.{item}"))//文件的路径,程序集.路径.文件名 
                    {
                        using (StreamReader sr = new StreamReader(sm))
                        {
                            //读取
                            stringBuilder.Append(sr.ReadToEnd().Replace(ExtensionProvider.SCRIPT_CODEID_PLACEHOLDER_STR, this.CodeId));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE Exception");
            }
            return stringBuilder.ToString();
        }
        private string LoadContentSizzle()
        {
            string[] strArray = { "sizzle.js", "json2.js" };
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                foreach (var item in strArray)
                {
                    using (Stream sm = asm.GetManifestResourceStream($"II.RPA.WebBrowser.Common.ExtensionScripts.{item}"))//文件的路径,程序集.路径.文件名 
                    {
                        using (StreamReader sr = new StreamReader(sm))
                        {
                            //读取
                            stringBuilder.Append(sr.ReadToEnd().Replace(ExtensionProvider.SCRIPT_CODEID_PLACEHOLDER_STR, this.CodeId));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE Exception");
            }
            return stringBuilder.ToString();
        }
        private string LoadContentIECode()
        {
            string[] strArray = { "commonscript.js", "dataCollectionScript.js", "utils.js" };
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                foreach (var item in strArray)
                {
                    using (Stream sm = asm.GetManifestResourceStream($"II.RPA.WebBrowser.Common.ExtensionScripts.{item}"))//文件的路径,程序集.路径.文件名 
                    {
                        using (StreamReader sr = new StreamReader(sm))
                        {
                            //读取
                            stringBuilder.Append(sr.ReadToEnd().Replace(ExtensionProvider.SCRIPT_CODEID_PLACEHOLDER_STR, this.CodeId));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE Exception");
            }
            return stringBuilder.ToString();
        }
    }
}

