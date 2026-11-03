using System;
using System.Collections.Generic;
using System.Linq;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    public static class CefResult
    {
        public static CefResult<T> Ok<T>(T value)
        {
            return new CefResult<T>(value);
        }

        public static CefResult<T> Error<T>(CefError error)
        {
            return new CefResult<T>(error);
        }

        public static CefResult<CefUnit> Unit()
        {
            return CefResult.Ok<CefUnit>(default(CefUnit));
        }

        public static CefResult<T> FromNullable<T>(T? value) where T : struct
        {
            if (value != null)
            {
                return CefResult.Ok<T>(value.Value);
            }
            return CefResult.Error<T>(CefErrorType.NoValue, "nullable did not contain a value");
        }

        public static CefResult<T> FromNullable<T>(T value) where T : class
        {
            if (value != null)
            {
                return CefResult.Ok<T>(value);
            }
            return CefResult.Error<T>(CefErrorType.NoValue, "reference was null.");
        }

        public static CefResult<T[]> FromEnumerable<T>(IEnumerable<T> sequence)
        {
            if (sequence == null || !sequence.Any<T>())
            {
                return CefResult.Error<T[]>(CefErrorType.NotFound);
            }
            return CefResult.Ok<T[]>(sequence.ToArray<T>());
        }

        public static CefResult<T> Unknown<T>()
        {
            return CefResult.Unknown<T>(null);
        }

        public static CefResult<T> Unknown<T>(string message)
        {
            return CefResult.Error<T>(CefErrorType.NotFound, message);
        }

        public static CefResult<T> Error<T>(string message)
        {
            return CefResult.Error<T>(CefErrorType.Fail, message);
        }

        public static CefResult<T> Error<T>(CefErrorType type)
        {
            return CefResult.Error<T>(type, null);
        }

        public static CefResult<T> Error<T>(CefErrorType type, string message)
        {
            return CefResult.Error<T>(new CefError(type, message));
        }
    }
}
