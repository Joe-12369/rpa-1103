using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OutLook.Models
{
    public class MailInfo
    {
        public string Account { get; set; }
        public List<MailAttachment> Attach { get; set; } = new List<MailAttachment>();
        public string BCC { get; set; }
        public string Body { get; set; }
        public string CC { get; set; }
        public string Filter { get; set; }
        public string Folder { get; set; }
        public string RecvBy { get; set; }
        public string RecvTime { get; set; }
        public string SendBy { get; set; }
        public string SendByEmail { get; set; }
        public string Subject { get; set; }
        public string EntryID { get; set; }
    }
}
