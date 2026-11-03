using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;


namespace II.Framework.Utility
{
    public static class WMIHelper
    {
        /// <summary>
        /// 异步执行wql查询
        /// </summary>
        /// <param name="aWQLString">
        /// e.g. SELECT SystemName FROM Win32_NetworkAdapter WHERE Description LIKE 'BROAD%'
        /// e.g. SELECT Caption FROM Win32_OperatingSystem
        /// </param>
        /// <returns></returns>
        public static Task<Dictionary<string, object>> ExecuteWqlObjectQueryAsync(string aWQLString)
        {
            return Task.Run(() =>
            {
                return ExecuteWqlObjectQueryCore(aWQLString);
            });
        }
        /// <summary>
        /// 执行wql查询
        /// </summary>
        /// <param name="aWQLString"></param>
        /// <returns></returns>
        public static Dictionary<string, object> ExecuteWqlObjectQuery(string aWQLString)
        {
            return ExecuteWqlObjectQueryCore(aWQLString);
        }
        /// <summary>
        /// 在wql的查询结果中找到指定属性的value
        /// </summary>
        /// <param name="aWQLString"></param>
        /// <param name="aPropertyName"></param>
        /// <returns></returns>
        public static object ExecuteWqlObjectQueryWithPropertyFilter(string aWQLString, string aPropertyName)
        {
            Dictionary<string, object> result = ExecuteWqlObjectQueryCore(aWQLString);
            if (result.TryGetValue(aPropertyName, out object valueObject))
            {
                return valueObject;
            }
            else
            {
                throw new ApplicationException($"can not find {aPropertyName} in the result of {aWQLString}");
            }
        }
        private static Dictionary<string, object> ExecuteWqlObjectQueryCore(string aWQLString)
        {
            WqlObjectQuery query = new WqlObjectQuery(aWQLString);
            ManagementObjectCollection resultCollection = new ManagementObjectSearcher(query).Get();
            if (resultCollection != null && resultCollection.Count > 0)
            {
                IEnumerable<ManagementObject> resultEnumerableT = resultCollection.Cast<ManagementObject>();
                return resultEnumerableT.FirstOrDefault().Properties.Cast<PropertyData>().ToDictionary(key => key.Name, value => value.Value);
            }
            else
            {
                throw new ApplicationException($"Nothing found in {aWQLString}");
            }
        }
    }
}
