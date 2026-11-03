using II.Framework.Resource;
using System;
using static II.Framework.Resource.I18nExtensions;
namespace II.Framework
{
    public class AppMeta
    {
        public static readonly AppMeta Default = new();
        private AppMeta() 
        {
            //设置语言
            var culture = II.Framework.Resource.LocalizationService.GetCurrentLanguage();
            Console.WriteLine($"Current Language:{culture.Name}");
            II.Framework.Resource.LocalizationService.SetCulture(culture);

            AppName = "执行小窗".I18n("AppMeta_63499A3A");
        }
        public string AppName
        {
            get;
        }
    }
}
