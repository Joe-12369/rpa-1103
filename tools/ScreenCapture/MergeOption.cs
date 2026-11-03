using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenCapture
{
    public class MergeOption
    {
        public string InputFile { get; set; }
        public string OutFile { get; set; }
        public string GetCutCommands()
        {
            string inputFile = InputFile.Replace(',','|').Replace(".mp4", ".ts");
            OutFile = Path.ChangeExtension(OutFile, "mp4");
            return $@" -i  concat:{inputFile} -acodec copy -vcodec copy -absf aac_adtstoasc {OutFile}";
        }

        public string GetTsCommands(string fileName)
        {
            string name = Path.ChangeExtension(fileName, "ts");
            return $" -i {fileName} -vcodec copy -vbsf h264_mp4toannexb {name}";
        }

        public void ClearTs(string fileName)
        {
            string name = Path.ChangeExtension(fileName, "ts");
            if (File.Exists(name))
            {
                File.Delete(name);
            }
        }
    }
}
