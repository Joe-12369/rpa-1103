using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.Utility
{
    public interface IInterlockedState<TState, TAction>
    {
        TState State { get; }
        IObservable<TState> StateObs { get; }
        void Fill(TAction action);
    }
}
