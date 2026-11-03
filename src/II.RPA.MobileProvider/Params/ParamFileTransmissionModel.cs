using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamFileTransmissionModel
    {
        public string sessionId { get; set; }
        public TransmissionType transmissionType { get; set; }
        public string macFilePath { get; set; }
        public string mobileDirPath { get; set; }
        public string mobileFilePath { get; set; }
        public string macDirPath { get; set; }
        public bool isReplaceSameNameFile { get; set; }
        public bool isAutoCreatePath { get; set; }
        public bool isDelSourceFile { get; set; }
    }
    public enum TransmissionType
    {
        /// <summary>
        /// 电脑到手机
        /// </summary>
        MacToMobile,
        /// <summary>
        /// 手机到电脑
        /// </summary>
        MobileToMac
    }
}
