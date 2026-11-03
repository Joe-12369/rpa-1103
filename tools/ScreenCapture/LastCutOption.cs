using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenCapture
{
    public class LastCutOption
    {
        public string InputFile { get; set; }
        public string OutFile { get; set; }
        public int Duration { get; set; }
        public TimeSpan Startime { get; set; }
        public string GetCutCommands()
        {
            OutFile = Path.ChangeExtension(OutFile, "mp4");
            return $@" -i {InputFile} -ss {Startime} -t {Duration} {OutFile}";
        }
    }
}
