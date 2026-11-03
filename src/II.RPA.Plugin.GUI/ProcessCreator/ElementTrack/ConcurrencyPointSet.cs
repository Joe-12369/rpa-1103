using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementTrack
{
    /// <summary>
    /// 防并发读写容器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrencyPointSet
    {
        private readonly AutoResetEvent m_DataUpdated = new AutoResetEvent(false);
        private readonly object m_Locker = new object();
        private Point? m_Data;

        public ConcurrencyPointSet()
        {

        }

        /// <summary>
        /// 存
        /// </summary>
        /// <param name="aData"></param>
        public void SetData(Point aData)
        {
            lock (m_Locker)
            {
                if (m_Data != aData)
                {
                    m_Data = aData;
                    m_DataUpdated.Set();
                }
            }
        }

        /// <summary>
        /// 取
        /// </summary>
        /// <returns></returns>
        public Point? TakeData()
        {
            Point? result;
            lock (m_Locker)
            {
                Point? t = m_Data;
                m_Data = null;
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
