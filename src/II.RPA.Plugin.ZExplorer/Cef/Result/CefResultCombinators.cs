using System;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    public static class CefResultCombinators
    {
        public static CefResult<T> Or<T>(this CefResult<T> left, CefResult<T> right)
        {
            if (left.IsSuccess)
            {
                return left;
            }
            return right;
        }

        public static CefResult<T> Or<T>(this CefResult<T> left, string message)
        {
            if (left.IsSuccess)
            {
                return left;
            }
            return CefResult.Error<T>(message);
        }

        public static CefResult<TValue> OrElse<TValue>(this CefResult<TValue> left, Func<CefError, CefResult<TValue>> continuation)
        {
            if (left.IsSuccess)
            {
                return left;
            }
            return continuation(left.Error.Value);
        }

        public static CefResult<TResult> And<TValue, TResult>(this CefResult<TValue> left, CefResult<TResult> right)
        {
            if (left.IsError)
            {
                return (CefResult<TResult>)left.ToErrorOf<TResult>();
            }
            return right;
        }

        public static CefResult<TResult> AndThen<TValue, TResult>(this CefResult<TValue> left, Func<TValue, CefResult<TResult>> continuation)
        {
            return left.AndIfThen((TValue _) => true, continuation);
        }

        public static CefResult<TResult> AndIfThen<TValue, TResult>(this CefResult<TValue> left, Func<TValue, bool> predicate, Func<TValue, CefResult<TResult>> continuation)
        {
            if (left.IsError)
            {
                return (CefResult<TResult>)left.ToErrorOf<TResult>();
            }
            if (!predicate(left.Success))
            {
                return CefResult.Error<TResult>(CefErrorType.Fail);
            }
            return continuation(left.Success);
        }
    }
}
