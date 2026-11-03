using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Java.Config
{
    internal class Configurations : ConfigurationBase<Configurations>
    {
        private Configurations()
        {
        }
        /// <summary>
        /// Java拾取模式  0:直接获取，1:逐层获取
        /// </summary>
        public JavaCaptureModeType JavaCaptureMode { get; set; }
    }
}
