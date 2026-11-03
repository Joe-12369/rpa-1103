using II.RPA.Logging;
using II.RPA.Plugin.OutLook.Params.Enum;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OutLook.Extension
{
    public static class MailExtension
    {
        private static ILogger logger = Logger.OfContext();
        public static void Dipose(object obj)
        {
            if (obj != null)
            {
                try
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                    obj = null;
                }
                catch
                {
                }
            }
        }
        /// <summary>
        /// 根据条件获取文件夹对象
        /// </summary>
        /// <param name="olApp"></param>
        /// <param name="e_MailboxFolder"></param>
        /// <param name="folderPath">格式：A-B-C </param>
        public static dynamic GetFolderByFilter(dynamic olApp, string mailAccount, E_MailboxFolder e_MailboxFolder, string folderPath)
        {
            try
            {
                dynamic retFolder = null;
                dynamic session = olApp.GetNamespace("mapi");
                dynamic accounts = session.Accounts;
                dynamic parentFolder = null;
                bool accountMatch = false;

                foreach (dynamic account in accounts)
                {
                    if (account.SmtpAddress == mailAccount)
                    {
                        accountMatch = true;

                        foreach (dynamic folder in session.Folders)
                        {
                            if (folder.Name == mailAccount)
                            {
                                parentFolder = folder;
                                break;
                            }
                        }

                        if (e_MailboxFolder == E_MailboxFolder.Inbox)
                        {
                            if (parentFolder != null)
                            {
                                //因为不同邮箱客户端可能【收件箱】显示名不一致，故通过索引查找，比较准确
                                //retFolder = parentFolder.Folders[2] as Outlook.Folder;
                                retFolder = parentFolder.Folders[2];
                            }
                        }
                        else if (e_MailboxFolder == E_MailboxFolder.Sented)
                        {
                            if (parentFolder != null)
                            {
                                //因为不同邮箱客户端可能【已发送邮箱】显示名不一致，故通过索引查找，比较准确
                                // retFolder = parentFolder.Folders[4] as Outlook.Folder;
                                retFolder = parentFolder.Folders[4];
                            }
                        }
                        else
                        {
                            dynamic allFolders = parentFolder.Folders;
                            string[] levels = folderPath.Split('/');
                            int initIndex = 0;
                            bool hasMatch = false;
                            while (initIndex < levels.Count() && (initIndex == 0 || hasMatch))
                            {
                                foreach (dynamic item in allFolders)
                                {
                                    if (item.Name == levels[initIndex])
                                    {
                                        if (initIndex == levels.Count() - 1)
                                        {
                                            retFolder = item;
                                        }
                                        else
                                        {
                                            allFolders = item.Folders;
                                        }
                                        hasMatch = true;
                                        break;
                                    }
                                }
                                initIndex++;
                            }
                        }
                        break;
                    }
                }
                if (!accountMatch)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundMailAccount);
                }
                return retFolder;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"获取账号对应的文件夹异常:{ex}");
                return null;
            }
        }
        public static void SetOutlookProperty<T>(T field, string name, object value)
        {
            try
            {
                if (value == null || value.ToString() == "")
                {
                    return;
                }
                Type type = typeof(T);
                type.InvokeMember(name.Trim(), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty, null, field, new object[] { value });
            }
            catch (Exception ex)
            {
                logger.LogWarning($"set excel property {name} exception: {ex}");
            }
        }
    }
}
