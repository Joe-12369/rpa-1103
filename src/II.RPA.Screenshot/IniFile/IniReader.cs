using II.RPA.Logging;
using Microsoft.Extensions.Logging;


using System.Collections.Generic;
using System.IO;
using System.Text;

namespace II.RPA.Screenshot.IniFile {
	/// <summary>
	/// 
	/// </summary>
	public static class IniReader {
		private const string SectionStart = "[";
		private const string SectionEnd = "]";
		private const string Comment = ";";
		private static readonly char[] Assignment = { '=' };

		/// <summary>
		/// Read an ini file to a Dictionary, each key is a section and the value is a Dictionary with name and values.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		public static IDictionary<string, IDictionary<string, string>> Read(string path, Encoding encoding) {
			var ini = new Dictionary<string, IDictionary<string, string>>();
			using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024)) {
				using (var streamReader = new StreamReader(fileStream, encoding)) {
					IDictionary<string, string> nameValues = new Dictionary<string, string>();
					while (!streamReader.EndOfStream) {
						string line = streamReader.ReadLine();
						if (line == null)
						{
							continue;
						}
						string cleanLine = line.Trim();
						if (cleanLine.Length == 0 || cleanLine.StartsWith(Comment)) {
							continue;
						}
						if (cleanLine.StartsWith(SectionStart)) {
							string section = line.Replace(SectionStart, string.Empty).Replace(SectionEnd, string.Empty).Trim();
							if (!ini.TryGetValue(section, out nameValues))
							{
								nameValues = new Dictionary<string, string>();
								ini.Add(section, nameValues);
							}
						} else {
							string[] keyvalueSplitter = line.Split(Assignment, 2);
							string name = keyvalueSplitter[0];
							string inivalue = keyvalueSplitter.Length > 1 ? keyvalueSplitter[1] : null;
							if (nameValues.ContainsKey(name)) {
								nameValues[name] = inivalue;
							} else {
								nameValues.Add(name, inivalue);
							}
						}
					}
				}
			}
			return ini;
		}
	}
}
