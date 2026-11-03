using II.RPA.Logging;
using II.RPA.Plugin.Core.PluginBase;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core
{
    public class NeedResetObjectManager
    {
        private static ILogger logger = Logger.OfContext();

        private static List<INeedReset> _needResetObjects = null;
        static NeedResetObjectManager()
        {
            _needResetObjects = new List<INeedReset>();
        }
        public static void AddNeedResetObject(INeedReset obj)
        {
            _needResetObjects.Add(obj);
        }
        public static void RemoveNeedResetObject(INeedReset obj)
        {
            _needResetObjects.Remove(obj);
        }
        public static void ResetAllObjects(bool isSamePackage)
        {
            var needResetObjects = _needResetObjects.Where(o => o != null).ToList();
            for (int i = 0; i < needResetObjects.Count; i++)
            {
                try
                {
                    logger.LogDebug($"Reset Object Type:{needResetObjects[i]?.GetType().ToString()}");
                    needResetObjects[i]?.Reset(isSamePackage);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"重置数据发成异常情况，警惕");
                }
            }
        }
    }
}

