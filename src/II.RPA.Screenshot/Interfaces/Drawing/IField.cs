using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System;
using System.ComponentModel;

namespace II.RPA.Screenshot.Plugin.Interfaces.Drawing
{
	[Flags]
	public enum FieldFlag
	{
		NONE = 0,
		CONFIRMABLE = 1,
		COUNTER = 2
	}
	public interface IFieldType
	{
		string Name
		{
			get;
			set;
		}
	}

	public interface IField : INotifyPropertyChanged
	{
		object Value
		{
			get;
			set;
		}
		IFieldType FieldType
		{
			get;
			set;
		}
		string Scope
		{
			get;
			set;
		}
		bool HasValue
		{
			get;
		}
	}
	/// <summary>
	/// EventHandler to be used when a field value changes
	/// </summary>
	public delegate void FieldChangedEventHandler(object sender, FieldChangedEventArgs e);

	/// <summary>
	/// EventArgs to be used with FieldChangedEventHandler
	/// </summary>
	public class FieldChangedEventArgs : EventArgs
	{
		public IField Field
		{
			get;
			private set;
		}
		public FieldChangedEventArgs(IField field)
		{
			Field = field;
		}
	}
}
