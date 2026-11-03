using System;
using System.Collections.Concurrent;
using System.Threading;

namespace II.RPA.Core.IPCExtension.Message
{
    public interface IPublisher
    {
        void Publish(string Msgkey, string message);
    }

    public interface ISubscriber
    {
        string Add(Subject sub);

        bool Remove(string storeKey);

        string SubscribeSync(string key, int timeout);

        string SubscribeSync(string key);
    }

    public class MessageBroker : IPublisher, ISubscriber
    {
        private ConcurrentDictionary<string, Subject> subscribers = new ConcurrentDictionary<string, Subject>();

        public string Add(Subject sub)
        {
            if (sub == null)
            {
                throw new NullReferenceException("订阅者不能为空");
            }

            string storeKey = Guid.NewGuid().ToString();
            subscribers.TryAdd(storeKey, sub);
            return storeKey;
        }

        public void Publish(string msgkey, string message)
        {
            if (string.IsNullOrEmpty(msgkey))
            {
                throw new NullReferenceException("消息键不能为空");
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new NullReferenceException("消息不能为空");
            }

            var scanItems = this.subscribers.ToArray();

            foreach (var subItem in scanItems)
            {
                if (subItem.Value.Key == msgkey)
                {
                    subItem.Value.PushMessage(message);
                }
            }
        }

        public bool Remove(string storeKey)
        {
            Subject subRet = null;
            return subscribers.TryRemove(storeKey, out subRet);
        }

        public string SubscribeSync(string key, int timeout)
        {
            using (Subject sub = new Subject(key, this))
            {
                sub.Pending(timeout);
                string value = sub.Value;
                return value;
            }
        }

        public string SubscribeSync(string key)
        {
            return this.SubscribeSync(key, -1);
        }
    }

    public class Subject : IDisposable
    {
        private string storeKey;
        private ISubscriber subscriber;

        public Subject(string key, ISubscriber subscriber) : this(key)
        {
            this.subscriber = subscriber;
            this.storeKey = this.subscriber.Add(this);
        }

        internal Subject(string key)
        {
            this.Key = key;
            this.AutoEvent = new AutoResetEvent(false);
        }

        public string Key { get; private set; }
        public string Value { get; private set; }
        internal AutoResetEvent AutoEvent { get; private set; }

        public void Dispose()
        {
            this.AutoEvent.Dispose();
            if (!string.IsNullOrEmpty(this.storeKey))
            {
                this.subscriber.Remove(this.storeKey);
                this.subscriber = null;
            }
        }

        public void Pending(int timeout)
        {
            if (!this.AutoEvent.WaitOne(timeout))
            {
                throw new TimeoutException($"通讯消息订阅超时，超时时间为:{timeout}.");
            }
        }

        internal void PushMessage(string value)
        {
            try
            {
                this.Value = value;
                this.AutoEvent.Set();
            }
            catch
            { }
        }
    }
}