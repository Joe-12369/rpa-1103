using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenCapture
{
    public class NameParser
    {
        public NameParserType Type { get; private set; }
        public int MaxNameLength { get; set; }
        public int MaxTitleLength { get; set; }
        public int AutoIncrementNumber { get; set; } // %i, %ia, %ib, %iAa, %ix
        public int ImageWidth { get; set; } // %width
        public int ImageHeight { get; set; } // %height
        public string WindowText { get; set; } // %t
        public string ProcessName { get; set; } // %pn
        public TimeZoneInfo CustomTimeZone { get; set; }

        // If we're trying to preview via TaskSettings or not
        // Used so that %rf throws "File not found" exceptions and brings up a popup on upload
        // But only returns an error message when previewing to avoid popup spam
        public bool IsPreviewMode { get; set; } = false;

        protected NameParser()
        {
        }

        public NameParser(NameParserType nameParserType)
        {
            Type = nameParserType;
        }

        private IEnumerable<Tuple<string, string[]>> ListEntryWithArguments(string text, string entry, int elements)
        {
            foreach (Tuple<string, string> o in text.ForEachBetween(entry + "{", "}"))
            {
                string[] s = o.Item2.Split(',');
                if (elements > s.Length)
                {
                    Array.Resize(ref s, elements);
                }
                yield return new Tuple<string, string[]>(o.Item1, s);
            }
        }

        private IEnumerable<Tuple<string, string>> ListEntryWithArgument(string text, string entry)
        {
            foreach (Tuple<string, string[]> o in ListEntryWithArguments(text, entry, 1))
            {
                yield return new Tuple<string, string>(o.Item1, o.Item2[0]);
            }
        }

        private IEnumerable<Tuple<string, int[]>> ListEntryWithValues(string text, string entry, int elements)
        {
            foreach (Tuple<string, string[]> o in ListEntryWithArguments(text, entry, elements))
            {
                int[] a = new int[o.Item2.Length];
                for (int i = o.Item2.Length - 1; i >= 0; --i)
                {
                    if (int.TryParse(o.Item2[i], out int n))
                    {
                        a[i] = n;
                    }
                }
                yield return new Tuple<string, int[]>(o.Item1, a);
            }
        }

        private IEnumerable<Tuple<string, int>> ListEntryWithValue(string text, string entry)
        {
            foreach (Tuple<string, int[]> o in ListEntryWithValues(text, entry, 1))
            {
                yield return new Tuple<string, int>(o.Item1, o.Item2[0]);
            }
        }
    }

    public enum NameParserType
    {
        Default,
        Text, // Allows new line
        FileName,
        FolderPath,
        FilePath,
        URL // URL path encodes
    }
}
