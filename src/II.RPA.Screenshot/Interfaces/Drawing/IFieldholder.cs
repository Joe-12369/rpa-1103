using II.RPA.Logging;
using Microsoft.Extensions.Logging;

using System.Collections.Generic;

namespace II.RPA.Screenshot.Plugin.Interfaces.Drawing
{
	/// <summary>
	/// Any element holding Fields must provide access to it.
	/// AbstractFieldHolder is the basic implementation.
	/// If you need the fieldHolder to have child fieldHolders,
	/// you should consider using IFieldHolderWithChildren.
	/// </summary>
	public interface IFieldHolder
	{

		event FieldChangedEventHandler FieldChanged;

		void AddField(IField field);
		void RemoveField(IField field);
		IList<IField> GetFields();
		IField GetField(IFieldType fieldType);
		bool HasField(IFieldType fieldType);
		void SetFieldValue(IFieldType fieldType, object value);
	}

	/// <summary>
	/// Extended fieldHolder which has fieldHolder children.
	/// Implementations should pass field values to and from 
	/// their children.
	/// AbstractFieldHolderWithChildren is the basic implementation.
	/// </summary>
	public interface IFieldHolderWithChildren : IFieldHolder
	{
		void AddChild(IFieldHolder fieldHolder);
		void RemoveChild(IFieldHolder fieldHolder);
	}
}
