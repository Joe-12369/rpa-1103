using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.JavaBridgeHost
{
    public abstract class ObservableObject
    {
        [JsonIgnore]
        public Dictionary<string, object> _backingFieldValues = new Dictionary<string, object>();
        public void SetBackingFieldValues(Dictionary<string, object> values)
        {
            _backingFieldValues = values;
        }
        protected T GetProperty<T>([CallerMemberName]string propertyName = null)
        {
            try
            {
                if (propertyName == null)
                {
                    throw new ArgumentNullException(nameof(propertyName));
                }
                object value;
                if (_backingFieldValues.TryGetValue(propertyName, out value))
                {
                    return (T)value;
                }
                return default(T);
            }
            catch (Exception ex)
            {
                NLogger.Default.Error(ex.ToString());
                throw;
            }
        }

        protected bool SetProperty<T>(T newValue, [CallerMemberName]string propertyName = null)
        {
            try
            {
                if (propertyName == null)
                {
                    throw new ArgumentNullException(nameof(propertyName));
                }
                if (IsEqual(GetProperty<T>(propertyName), newValue)) return false;
                _backingFieldValues[propertyName] = newValue;
                Type typeParameterType = typeof(T);
                if (typeParameterType.Name.ToLower().Contains("readonly"))
                {
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }

        private bool IsEqual<T>(T field, T newValue)
        {
            return Equals(field, newValue);
        }

        private string GetNameFromExpression<T>(Expression<Func<T>> selectorExpression)
        {
            var body = (MemberExpression)selectorExpression.Body;
            var propertyName = body.Member.Name;
            return propertyName;
        }
    }
}
