using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Observable.Reactives
{
    public abstract class ReactiveObject<TS> : INotifyPropertyChanged
        where TS : ReactiveObject<TS>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual bool SetProperty<T>(
            ref T storage,
            T value,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            RaisePropertyChanged(propertyName);

            return true;
        }

        protected virtual bool SetProperty<T>(
            ref T storage,
            T value,
            Action onChanged,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            onChanged?.Invoke();
            RaisePropertyChanged(propertyName);

            return true;
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(this, args);
        }

        protected IObservable<TV> Obs<TV>(
            Expression<Func<TS, TV>> selector,
            bool withInit = false)
        {
            return (this as TS).Watch(selector, withInit);
        }
    }
}
