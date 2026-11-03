using System.Threading;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementTrack
{
    /// <summary>
    /// 防并发读写容器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrencyItemContainer<T>
    {
        private readonly AutoResetEvent m_DataUpdated = new AutoResetEvent(false);
        private readonly object m_Locker = new object();
        private T m_Data;

        public ConcurrencyItemContainer()
        {

        }

        /// <summary>
        /// 存
        /// </summary>
        /// <param name="aData"></param>
        public void SetData(T aData)
        {
            lock (m_Locker)
            {
                m_Data = aData;
                m_DataUpdated.Set();
            }
        }

        /// <summary>
        /// 取
        /// </summary>
        /// <returns></returns>
        public T TakeData()
        {
            T result;
            lock (m_Locker)
            {
                T t = m_Data;
                m_Data = default;
                result = t;
            }
            return result;
        }

        /// <summary>
        /// 等待数据
        /// </summary>
        /// <param name="aMs"></param>
        /// <returns></returns>
        public bool WaitForData(int aMs)
        {
            return m_DataUpdated.WaitOne(aMs);
        }
    }
}
