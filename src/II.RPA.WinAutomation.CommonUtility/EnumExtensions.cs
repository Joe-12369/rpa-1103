using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility
{
    public static class EnumExtensions
    {
        private static readonly ConcurrentDictionary<Enum, string> _descriptionCache = new ConcurrentDictionary<Enum, string>();

        public static string ToDescription(this Enum value)
        {
            return _descriptionCache.GetOrAdd(value, key =>
            {
                var field = key.GetType().GetField(key.ToString());
                if (field == null) return key.ToString();

                var attr = field.GetCustomAttribute<DescriptionAttribute>();
                return attr?.Description ?? key.ToString();
            });
        }
    }
}
