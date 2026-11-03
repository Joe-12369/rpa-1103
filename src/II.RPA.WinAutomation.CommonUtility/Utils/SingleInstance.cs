using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility.Utils
{
    #region
    /// <summary>
    /// 泛型单例实现基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingleInstance<T> where T : class, new()
    {
        private static readonly object _lock = new object();

        private static T Instance;
        public static T GetInstance()
        {
            if (Instance == null)
            {
                lock (_lock)
                {
                    if (Instance == null)
                    {
                        Instance = new T();
                    }
                }
            }
            return Instance;
        }
    }

    #endregion
}
