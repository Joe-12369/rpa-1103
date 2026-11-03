using System;
using System.Diagnostics;

namespace II.RPA.MobileDeviceManager.Win32
{
	[DebuggerDisplay("{DebuggerDisplay}")]
	public readonly struct PCWSTR : IEquatable<PCWSTR>
	{
		public unsafe readonly char* Value;

		public unsafe int Length
		{
			get
			{
				char* p = Value;
				if (p == null)
				{
					return 0;
				}
				for (; *p != 0; p++)
				{
				}
				return checked((int)(p - Value));
			}
		}

		private string DebuggerDisplay => ToString();

		public unsafe PCWSTR(char* value)
		{
			Value = value;
		}

		public unsafe static explicit operator char*(PCWSTR value)
		{
			return value.Value;
		}

		public unsafe static implicit operator PCWSTR(char* value)
		{
			return new PCWSTR(value);
		}

		public unsafe static implicit operator PCWSTR(PWSTR value)
		{
			return new PCWSTR(value.Value);
		}

		public unsafe bool Equals(PCWSTR other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			if (obj is PCWSTR other)
			{
				return Equals(other);
			}
			return false;
		}

		public unsafe override int GetHashCode()
		{
			return (int)Value;
		}

		public unsafe override string ToString()
		{
			if (Value != null)
			{
				return new string(Value);
			}
			return null;
		}

		public unsafe ReadOnlySpan<char> AsSpan()
		{
			if (Value != null)
			{
				return new ReadOnlySpan<char>(Value, Length);
			}
			return default(ReadOnlySpan<char>);
		}
	}
}
