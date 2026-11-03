using System;
using System.Diagnostics;

namespace II.RPA.MobileDeviceManager.Win32
{
	[DebuggerDisplay("{Value}")]
	public readonly struct PWSTR : IEquatable<PWSTR>
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

		public unsafe PWSTR(char* value)
		{
			Value = value;
		}

		public unsafe static implicit operator char*(PWSTR value)
		{
			return value.Value;
		}

		public unsafe static implicit operator PWSTR(char* value)
		{
			return new PWSTR(value);
		}

		public unsafe static bool operator ==(PWSTR left, PWSTR right)
		{
			return left.Value == right.Value;
		}

		public static bool operator !=(PWSTR left, PWSTR right)
		{
			return !(left == right);
		}

		public unsafe bool Equals(PWSTR other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			if (obj is PWSTR other)
			{
				return Equals(other);
			}
			return false;
		}

		public unsafe override int GetHashCode()
		{
			return checked((int)Value);
		}

		public unsafe override string ToString()
		{
			if (Value != null)
			{
				return new string(Value);
			}
			return null;
		}

		public unsafe Span<char> AsSpan()
		{
			if (Value != null)
			{
				return new Span<char>(Value, Length);
			}
			return default(Span<char>);
		}
	}
}
