using II.RPA.Core.Server.Attributes;
using II.RPA.Core.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static II.RPA.Core.Server.Models.Config.ServiceBootSettingsSection;

namespace II.RPA.Core.Server.Models.Config
{
    public static class ConfigurationHelper
    {
        public readonly static Dictionary<string, string> AppSettings = new Dictionary<string, string>();
        static ConfigurationHelper()
        {
            AppSettings = ConfigurationManager.AppSettings.AllKeys.ToDictionary(key => key, value => ConfigurationManager.AppSettings.Get(value));
        }
        public static string GetAppSettingByKey(string aKey)
        {
            if (AppSettings.TryGetValue(aKey, out string value))
            {
                return value;
            }
            else
            {
                throw new ArgumentException($"Invalid key: {aKey}");
            }
        }
        public static IEnumerable<ServiceBootModel> GetConfigServiceBootTypes()
        {
            string bootKeys = GetAppSettingByKey("ServiceBoots");
            string[] bootKeyAarry = bootKeys.Split(',');
            var bootsection = GetSettingsSection<ServiceBootSettingsSection>("ServiceBootSettings");
            var boots = bootsection.ServiceBootSettings;
            return boots.Cast<ServiceBootSetting>().Where(b 
                => 
                bootKeyAarry.Contains(b.BootKey)).Select(run 
                    => 
                    new ServiceBootModel 
                    {
                        ServiceBootName = run.BootKey,
                        ServiceBootType = ReflectionUtils.GetEnumAttribute<ServiceBootTypeAttribute>(run.BootType).ServiceBootType,
                        ServiceBootParams = run.BootParams.Split(','),
                    });
        }
        public static T GetSettingsSection<T>(string aSectionName) where T: ConfigurationSection
        {
            T sectionT = ConfigurationManager.GetSection(aSectionName) as T;
            return sectionT;
        }
    }
}
