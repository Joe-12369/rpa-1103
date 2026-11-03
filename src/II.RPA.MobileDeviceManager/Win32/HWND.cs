using System;
using System.Diagnostics;

namespace II.RPA.MobileDeviceManager.Win32
{

    [DebuggerDisplay("{Value}")]
    public readonly struct HWND : IEquatable<HWND>
    {
        public readonly IntPtr Value;

        public HWND(IntPtr value)
        {
            Value = value;
        }

        public static implicit operator IntPtr(HWND value)
        {
            return value.Value;
        }

        public static explicit operator HWND(IntPtr value)
        {
            return new HWND(value);
        }

        public static bool operator ==(HWND left, HWND right)
        {
            return left.Value == right.Value;
        }

        public static bool operator !=(HWND left, HWND right)
        {
            return !(left == right);
        }

        public bool Equals(HWND other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj is HWND other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
