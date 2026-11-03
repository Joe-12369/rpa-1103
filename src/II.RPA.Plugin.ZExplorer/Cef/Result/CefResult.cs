using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    [Serializable]
    public struct CefResult<T>
    {
        internal CefResult(CefError error)
        {
            this.Success = default(T);
            this.Error = error;
        }

        internal CefResult(T success)
        {
            this.Success = success;
            this.Error = null;
        }

        public T Success { get; private set; }

        public CefError? Error { get; private set; }

        public bool IsError
        {
            get
            {
                return this.Error != null;
            }
        }

        public bool IsSuccess
        {
            get
            {
                return !this.IsError;
            }
        }

        public T UnwrapOr(T value)
        {
            if (!this.IsSuccess)
            {
                return value;
            }
            return this.Success;
        }

        public object ToErrorOf<TResult>()
        {
            return new CefResult<T>(this.Error.Value);
        }

        public override bool Equals(object obj)
        {
            if (obj is CefResult<T>)
            {
                CefResult<T> other = (CefResult<T>)obj;
                return this.Equals(other);
            }
            return false;
        }

    }
}
