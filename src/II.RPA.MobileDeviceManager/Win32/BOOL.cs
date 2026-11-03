namespace II.RPA.MobileDeviceManager.Win32
{
	public readonly struct BOOL
	{
		private readonly int value;

		public int Value => value;

		public BOOL(bool value)
		{
			this.value = (value ? 1 : 0);
		}

		public BOOL(int value)
		{
			this.value = value;
		}

		public static implicit operator bool(BOOL value)
		{
			sbyte v = checked((sbyte)value.value);
			return (byte)v != 0;
		}

		public static implicit operator BOOL(bool value)
		{
			return new BOOL(value);
		}

		public static explicit operator BOOL(int value)
		{
			return new BOOL(value);
		}
	}
}
