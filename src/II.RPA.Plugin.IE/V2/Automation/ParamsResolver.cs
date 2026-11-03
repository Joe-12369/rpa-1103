using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.IE.V2.Automation
{
    internal class ParamsResolver
    {
        private DynamicViewDataDictionary _dynamicViewDataDictionary;
        private ViewDataDictionary _viewDataDictionary;
        public ViewDataDictionary ViewData
        {
            get
            {
                if (_viewDataDictionary == null)
                {
                    _viewDataDictionary = new ViewDataDictionary(StringComparer.OrdinalIgnoreCase);
                }
                return _viewDataDictionary;
            }
            set { _viewDataDictionary = value; }
        }
        public dynamic ViewBag
        {
            get
            {
                if (_dynamicViewDataDictionary == null)
                {
                    _dynamicViewDataDictionary = new DynamicViewDataDictionary(() => ViewData);
                }

                return _dynamicViewDataDictionary;
            }
        }
    }
    internal class ViewDataDictionary : ConcurrentDictionary<string, object>
    {
        internal ViewDataDictionary()
        {

        }
        internal ViewDataDictionary(IEqualityComparer<string> comparer) : base(comparer)
        {

        }
    }
    internal sealed class DynamicViewDataDictionary : DynamicObject
    {
        private readonly Func<ViewDataDictionary> _viewDataThunk;

        public DynamicViewDataDictionary(Func<ViewDataDictionary> viewDataThunk)
        {
            _viewDataThunk = viewDataThunk;
        }

        private ViewDataDictionary ViewData
        {
            get
            {
                ViewDataDictionary viewData = _viewDataThunk();
                Debug.Assert(viewData != null);
                return viewData;
            }
        }

        // Implementing this function improves the debugging experience as it provides the debugger with the list of all
        // the properties currently defined on the object
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return ViewData.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = ViewData[binder.Name];
            // since ViewDataDictionary always returns a result even if the key does not exist, always return true
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            ViewData[binder.Name] = value;
            // you can always set a key in the dictionary so return true
            return true;
        }
    }


}



