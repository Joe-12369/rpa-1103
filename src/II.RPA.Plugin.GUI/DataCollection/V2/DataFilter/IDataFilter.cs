using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataFilter
{
    internal interface IDataFilter
    {
        bool Execute(string content, FilterConfig config);
    }
    internal abstract class DataFilter<T> : IDataFilter where T : IDataFilter, new()
    {
        private readonly static Lazy<T> _instance = new Lazy<T>(() => new T());
        public static T Instance => _instance.Value;

        public abstract bool Execute(string content, FilterConfig config);
    }
}
