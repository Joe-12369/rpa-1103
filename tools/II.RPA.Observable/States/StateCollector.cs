using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Observable.Reactives;
using II.RPA.Observable.Scheduling;

namespace II.RPA.Observable.States
{
    public abstract class StateCollector<T> :
        CtsControllable,
        IStateCollector<T>
        where T : ReactiveObject<T>, new()
    {
        public T State { get; } = new T();
    }
}
