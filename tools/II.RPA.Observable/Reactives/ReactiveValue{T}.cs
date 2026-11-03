using System;

namespace II.RPA.Observable.Reactives
{
    public class ReactiveValue<T> : ReactiveObject<ReactiveValue<T>>
    {
        public ReactiveValue()
        {
            ContentObs = new Lazy<IObservable<T>>(() => this.Obs(x => x.Content));
        }

        #region Rx-Value

        private T content;

        public T Content
        {
            get { return content; }
            set { SetProperty(ref content, value); }
        }

        #endregion

        public Lazy<IObservable<T>> ContentObs { get; }
    }
}
