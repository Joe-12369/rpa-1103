using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Observable.Reactives;
using II.RPA.Observable.Scheduling;

namespace II.RPA.Observable.States
{
    public interface IStateCollector<T> : IControllable
        where T : ReactiveObject<T>
    {
        T State { get; }
    }
}
