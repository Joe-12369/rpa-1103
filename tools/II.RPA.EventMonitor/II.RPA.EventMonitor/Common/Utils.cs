using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using Newtonsoft.Json;
namespace II.RPA.EventMonitor
{
    internal static class Utils
    {
        internal static T ToObject<T>(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return default(T);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(str);
        }
        internal static T ToObject<T>(this object obj)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(obj.ToJson());
        }
        internal static string ToJson(this object obj)
        {
            if (obj == null)
                return "";
            return JsonConvert.SerializeObject(obj);
        }

        internal static int ToInt32(this object obj, int defValue = 0)
        {
            if (obj == null)
                return defValue;
            int val;
            if (int.TryParse(obj.ToString(), out val))
            {
                return val;
            }
            return defValue;
        }


        #region 进程 相关
        internal static bool IsExistsSameProcess()
        {
            string pName = Process.GetCurrentProcess().ProcessName;
            Process[] processesByName = GetProcessesByName(pName, onlyCurrentUser: true);
            int curPid = Process.GetCurrentProcess().Id;
            return processesByName.Any(p => p.Id != curPid);
        }

        internal static void CloseSameProcessExceptSelf(string pName)
        {
            Process[] processesByName = GetProcessesByName(pName, onlyCurrentUser: true);
            int curPid = Process.GetCurrentProcess().Id;
            Process[] array = processesByName;
            foreach (Process process in array)
            {
                if (process.Id != curPid)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                }
            }
        }
        internal static Process[] GetProcessesByName(string processName, bool onlyCurrentUser)
        {
            if (!onlyCurrentUser)
            {
                return Process.GetProcessesByName(processName);
            }
            return (from m in Process.GetProcessesByName(processName)
                    where GetProcessOwner(m.Id) == Environment.UserName
                    select m).ToArray();
        }

        internal static string GetProcessOwner(int processId)
        {
            foreach (ManagementObject obj in new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID = " + processId).Get())
            {
                string[] argList = new string[2]
                {
                string.Empty,
                string.Empty
                };
                object[] args = argList;
                if (Convert.ToInt32(obj.InvokeMethod("GetOwner", args)) == 0)
                {
                    return argList[0];
                }
            }
            return "NO OWNER";
        }
        #endregion

        #region 邮件
        /// <summary>
        /// task取消时间
        /// </summary>
        private static readonly int CancelTokenMillSecends = 30000;
        /// <summary>
        /// 模拟邮件客户端信息
        /// </summary>
        /// <param name="imapClient"></param>
        internal static void Identify(this ImapClient imapClient)
        {
            List<ImapImplementation> Implements = new List<ImapImplementation>()
                {
                new ImapImplementation() {Name = "foxmail",Version = "7.2.9.156", },
                new ImapImplementation() {Name = "outlook",Version = "14.0.4760.1000", },
                };
            Func<ImapCapabilities, ImapCapabilities, bool> HasImapCapabilitiesId = (sourceFlag, targetFlag) => { return ((sourceFlag | targetFlag) == sourceFlag); };
            if (HasImapCapabilitiesId(imapClient.Capabilities, ImapCapabilities.Id))
            {
                var clientImplementation = new ImapImplementation
                {
                    Name = "foxmail",
                    Version = "7.2.9.156",//13.0.503.101
                };
                var TokenSource = new CancellationTokenSource(CancelTokenMillSecends);
                var serverImplementation = imapClient.Identify(clientImplementation, cancellationToken: TokenSource.Token);
            }
        }

        internal static int GetMessageCount(this ImapClient imapClient, IMailFolder folder, bool closeFolder = false)
        {
            if (imapClient == null)
                return 0;
            var TokenSource = new CancellationTokenSource(CancelTokenMillSecends);
            if (!folder.IsOpen)
                folder.Open(MailKit.FolderAccess.ReadWrite, cancellationToken: TokenSource.Token);
            var Count = folder.Count;
            if (closeFolder)
                folder.Close(cancellationToken: TokenSource.Token);

            return Count;
        }
        internal static int GetMessageCount(this ImapClient imapClient, IMailFolder folder, SearchQuery Query, bool closeFolder = false)
        {
            var Count = imapClient.GetManyMessageUids(folder, Query, closeFolder)?.Count;
            return Count ?? 0;
        }

        internal static List<UniqueId> GetManyMessageUids(this ImapClient imapClient, IMailFolder folder, SearchQuery Query, bool closeFolder = false)
        {
            if (imapClient == null)
                return null;
            //List<IMailFolder> mailFolderList = imapClient.GetFolders(imapClient.PersonalNamespaces[0]).ToList();//取所有文件夹
            if (folder == null)
                return null;
            var TokenSource = new CancellationTokenSource(CancelTokenMillSecends);
            if (!folder.IsOpen)
                folder.Open(FolderAccess.ReadWrite, cancellationToken: TokenSource.Token);
            var uids = folder.Search(Query, cancellationToken: TokenSource.Token);
            if (closeFolder)
                folder.Close(cancellationToken: TokenSource.Token);
            return uids?.ToList();
        }

        internal static MimeMessage GetMessage(this ImapClient imapClient, UniqueId Id, IMailFolder folder, bool closeFolder = false)
        {
            if (imapClient == null)
                return null;
            //List<IMailFolder> mailFolderList = imapClient.GetFolders(imapClient.PersonalNamespaces[0]).ToList();//取所有文件夹
            if (folder == null)
                return null;
            var TokenSource = new CancellationTokenSource(CancelTokenMillSecends);
            if (!folder.IsOpen)
                folder.Open(MailKit.FolderAccess.ReadWrite, cancellationToken: TokenSource.Token);
            var msg = folder.GetMessage(Id, cancellationToken: TokenSource.Token);
            if (closeFolder)
                folder.Close(cancellationToken: TokenSource.Token);
            return msg;
        }
        internal static MimeMessage GetMessage(this ImapClient imapClient, int Index, IMailFolder folder, bool closeFolder = false)
        {
            if (imapClient == null)
                return null;
            //List<IMailFolder> mailFolderList = imapClient.GetFolders(imapClient.PersonalNamespaces[0]).ToList();//取所有文件夹
            if (folder == null)
                return null;
            var TokenSource = new CancellationTokenSource(CancelTokenMillSecends);
            if (!folder.IsOpen)
                folder.Open(FolderAccess.ReadWrite, cancellationToken: TokenSource.Token);
            var msg = folder.GetMessage(Index, cancellationToken: TokenSource.Token);
            if (closeFolder)
                folder.Close(cancellationToken: TokenSource.Token);
            return msg;
        }

        internal static IList<IMessageSummary> FetchMessage(this ImapClient imapClient, List<UniqueId> Ids, IMailFolder folder, MessageSummaryItems Item, bool closeFolder = false)
        {
            if (imapClient == null)
                return null;
            //List<IMailFolder> mailFolderList = imapClient.GetFolders(imapClient.PersonalNamespaces[0]).ToList();//取所有文件夹
            if (folder == null)
                return null;
            var TokenSource = new CancellationTokenSource(CancelTokenMillSecends);
            if (!folder.IsOpen)
                folder.Open(FolderAccess.ReadWrite, cancellationToken: TokenSource.Token);
            var msg = folder.Fetch(Ids, Item, cancellationToken: TokenSource.Token);
            if (closeFolder)
                folder.Close(cancellationToken: TokenSource.Token);
            return msg;
        }
        internal static IList<IMessageSummary> FetchMessage(this ImapClient imapClient, List<int> indexes, IMailFolder folder, MessageSummaryItems Item, bool closeFolder = false)
        {
            if (imapClient == null)
                return null;
            //List<IMailFolder> mailFolderList = imapClient.GetFolders(imapClient.PersonalNamespaces[0]).ToList();//取所有文件夹
            if (folder == null)
                return null;
            var TokenSource = new CancellationTokenSource(CancelTokenMillSecends);
            if (!folder.IsOpen)
                folder.Open(FolderAccess.ReadWrite, cancellationToken: TokenSource.Token);
            var msg = folder.Fetch(indexes, Item, cancellationToken: TokenSource.Token);
            if (closeFolder)
                folder.Close(cancellationToken: TokenSource.Token);
            return msg;
        }

        internal static bool DeleteMessage(this ImapClient imapClient, UniqueId Id, IMailFolder folder, SearchQuery Query, bool closeFolder = false)
        {
            if (imapClient == null)
                return false;
            //List<IMailFolder> mailFolderList = imapClient.GetFolders(imapClient.PersonalNamespaces[0]).ToList();//取所有文件夹
            if (folder == null)
                return false;
            var TokenSource = new CancellationTokenSource(CancelTokenMillSecends);
            if (!folder.IsOpen)
                folder.Open(FolderAccess.ReadWrite, cancellationToken: TokenSource.Token);
            //设置状态
            folder.SetFlags(Id, MessageFlags.Deleted, false, cancellationToken: TokenSource.Token);
            //或者删除邮件
            folder.RemoveFlags(Id, MessageFlags.Deleted, false);
            //主要针对Exchange 让删除指令执行
            folder.Expunge(cancellationToken: TokenSource.Token);

            if (closeFolder)
                folder.Close(cancellationToken: TokenSource.Token);
            return true;
        }
        internal static bool DeleteMessage(this ImapClient imapClient, int EmailIndex, IMailFolder folder, SearchQuery Query, bool closeFolder = false)
        {
            if (imapClient == null)
                return false;
            //List<IMailFolder> mailFolderList = imapClient.GetFolders(imapClient.PersonalNamespaces[0]).ToList();//取所有文件夹
            if (folder == null)
                return false;
            var TokenSource = new CancellationTokenSource(CancelTokenMillSecends);
            if (!folder.IsOpen)
                folder.Open(FolderAccess.ReadWrite, cancellationToken: TokenSource.Token);
            //设置状态
            folder.SetFlags(EmailIndex, MessageFlags.Deleted, false, cancellationToken: TokenSource.Token);
            //或者删除邮件
            folder.RemoveFlags(EmailIndex, MessageFlags.Deleted, false, cancellationToken: TokenSource.Token);
            //主要针对Exchange 让删除指令执行
            folder.Expunge(cancellationToken: TokenSource.Token);

            if (closeFolder)
                folder.Close(cancellationToken: TokenSource.Token);
            return true;
        }

        #endregion


        #region MyRegion
        internal static bool IsContains(this string input, string pattern)
        {
            if (!pattern.StartsWith("*"))
            {
                pattern = "*" + pattern;
            }
            if (!pattern.EndsWith("*"))
            {
                pattern = pattern + "*";
            }
            pattern = $"^{Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".")}$";
            return Regex.IsMatch(input, pattern);
        }
        #endregion
    }
}
