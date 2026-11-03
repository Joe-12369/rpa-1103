using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;

namespace II.RPA.Screenshot.IniFile {
	/// <summary>
	/// Attribute for telling that this class is linked to a section in the ini-configuration
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class IniSectionAttribute : Attribute {
		public IniSectionAttribute(string name) {
			Name = name;
		}
		public string Description;
		public string Name { get; set; }
	}

	/// <summary>
	/// Attribute for telling that a field is linked to a property in the ini-configuration selection
	/// </summary>
	[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
	public class IniPropertyAttribute : Attribute {
		public IniPropertyAttribute() {
			Separator = ",";
		}
		public IniPropertyAttribute(string name) : this() {
			Name = name;
		}
		public string Description {
			get;
			set;
		}
		public string Separator {
			get;
			set;
		}
		public string DefaultValue {
			get;
			set;
		}
		public string LanguageKey {
			get;
			set;
		}
		// If Encrypted is set to true, the value will be decrypted on load and encrypted on save
		public bool Encrypted {
			get;
			set;
		}
		public bool FixedValue {
			get;
			set;
		}
		public bool Expert {
			get;
			set;
		}
		public bool ExcludeIfNull {
			get;
			set;
		}

		public string Name {
			get;
			set;
		}
	}
}
