using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Common
{
    public class AsyncMessageModel<T, P>
    {
        private P m_Output;
        private readonly int m_Timeout = -1;
        private readonly AutoResetEvent m_HandleSignal;
        public AsyncMessageModel(T anInput)
        {
            m_HandleSignal = new AutoResetEvent(false);
            Input = anInput;
        }
        public AsyncMessageModel(T anInput, 
            int aTimeout) : this(anInput)
        {
            m_Timeout = aTimeout;
        }
        public AsyncMessageModel(T anInput,
            string anActionName,
            int aTimeout = -1) : this(anInput, aTimeout)
        {
            ActionName = anActionName;
        }
        public string ActionName
        {
            get;
            private set;
        }
        public T Input
        {
            get;
            protected set;
        }
        public void SetOutput(P anOutput)
        {
            m_Output = anOutput;
            m_HandleSignal.Set();
        }
        public bool WaitOutput(int aMs, out P aOutput)
        {
            if (m_HandleSignal.WaitOne(aMs))
            {
                aOutput = m_Output;
                m_Output = default;
                return true;
            }
            else
            {
                aOutput = default;
                return false;
            }
        }
        public P GetOutput()
        {
            if (m_HandleSignal.WaitOne(m_Timeout))
            {
                P output = m_Output;
                m_Output = default;
                return output;
            }
            else
            {
                throw new TimeoutException($"GetOutput operation Timeout:{m_Timeout}ms");
            }
        }
        public P GetOutput(TimeSpan aTimeout)
        {
            if (m_HandleSignal.WaitOne(aTimeout))
            {
                P output = m_Output;
                m_Output = default;
                return output;
            }
            else
            {
                throw new TimeoutException($"GetOutput operation Timeout:{aTimeout.TotalMilliseconds}ms");
            }
        }
    }
}
