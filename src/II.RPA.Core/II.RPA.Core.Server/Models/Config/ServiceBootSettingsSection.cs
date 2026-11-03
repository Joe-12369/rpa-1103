using II.RPA.Core.Server.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Server.Models.Config
{
    public class ServiceBootSettingsSection : ConfigurationSection
    {
        private static readonly ConfigurationProperty s_property =
            new ConfigurationProperty(string.Empty, typeof(ServiceBootSettingCollection), null, ConfigurationPropertyOptions.IsDefaultCollection);

        [ConfigurationProperty("", Options = ConfigurationPropertyOptions.IsDefaultCollection)]
        public ServiceBootSettingCollection ServiceBootSettings
        {
            get
            {
                return (ServiceBootSettingCollection)base[s_property];
            }
        }


        [ConfigurationCollection(typeof(ServiceBootSetting))]
        public class ServiceBootSettingCollection : ConfigurationElementCollection
        {
            new public ServiceBootSetting this[string DeviceName]
            {
                get
                {
                    return (ServiceBootSetting)base.BaseGet(DeviceName);
                }
            }
            protected override ConfigurationElement CreateNewElement()
            {
                return new ServiceBootSetting();
            }
            protected override object GetElementKey(ConfigurationElement element)
            {
                return ((ServiceBootSetting)element).BootKey;
            }
        }
        public class ServiceBootSetting : ConfigurationElement
        {
            [ConfigurationProperty("BootKey", IsRequired = true)]
            public string BootKey
            {
                get { return this["BootKey"].ToString(); }
                set { this["BootKey"] = value; }
            }
            [ConfigurationProperty("BootType", IsRequired = true)]
            public ServiceBootTypeEnum BootType
            {
                get { return (ServiceBootTypeEnum)Enum.Parse(typeof(ServiceBootTypeEnum), this["BootType"].ToString()); }
                set { this["BootType"] = value; }
            }

            [ConfigurationProperty("BootParams", IsRequired = true)]
            public string BootParams
            {
                get { return this["BootParams"].ToString(); }
                set { this["BootParams"] = value; }
            }

        }
    }
}
