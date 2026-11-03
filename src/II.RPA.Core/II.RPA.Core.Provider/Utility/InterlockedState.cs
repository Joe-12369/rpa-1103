using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using static System.Reactive.Linq.Observable;

namespace II.RPA.Core.Provider.Utility
{
    public abstract class InterlockedState<TState, TAction> : IInterlockedState<TState, TAction>, IDisposable
    {
        #region Fields

        private readonly Subject<TAction> actions;
        private readonly BehaviorSubject<TState> state;

        #endregion

        #region Constructors

        public InterlockedState() : this(default) { }

        public InterlockedState(TState init)
        {
            this.actions = new Subject<TAction>();
            this.state = new BehaviorSubject<TState>(init);

            this.actions
                .ObserveOn(Scheduler.Default)
                .Subscribe(action =>
                {
                    this.state.TryGetValue(out var current);
                    this.state.OnNext(Change(current, action));
                });
        }

        #endregion

        #region Properties

        public TState State {
            get
            {
                state.TryGetValue(out var current);
                return current;
            }
        }

        public IObservable<TState> StateObs => state;

        #endregion

        #region Method

        protected abstract TState Change(TState current, TAction action);

        public void Fill(TAction action)
        {
            actions.OnNext(action);
        }

        ~InterlockedState()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            actions.OnCompleted();
            state.OnCompleted();

            actions.Dispose();
            state.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
