using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenCapture
{
    public class CutOption
    {
        public string InputFile { get; set; }
        public string OutFile { get; set; }
        public int Duration { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Second { get; set; }
        public string GetCutCommands()
        {
            OutFile = Path.ChangeExtension(OutFile, "mp4");
            return $@" -i {InputFile} -ss {Hour}:{Minute}:{Second} -t {Duration} {OutFile}";
        }
    }
}
