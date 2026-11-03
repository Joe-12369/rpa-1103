using II.RPA.Logging;
using II.RPA.Plugin.OutLook.Extension;
using II.RPA.Plugin.OutLook.Models;
using II.RPA.Plugin.OutLook.Params;
using II.RPA.Plugin.OutLook.Params.BaseParam;
using II.RPA.Plugin.OutLook.Params.Enum;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OutLook
{
    public class OutLookEmailHelper
    {
        private static dynamic olApp;
        public static dynamic folderBox;
        public static dynamic mail;
        public static List<dynamic> mails;
        public static dynamic olNameSpace;
        private static ILogger logger = Logger.OfContext();

        public static void CreateOutlookApp()
        {
            // 使用ProgId获取Outlook.Application的类型  
            Type outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType != null)
            {
                olApp = Activator.CreateInstance(outlookType);
            }
            else
            {
                throw new Exception("Outlook is not installed.");
            }
        }
        /// <summary>
        /// 获取本地outlook客户端所有的账号邮箱
        /// </summary>
        /// <returns></returns>
        public static List<string> CheckAccount()
        {
            try
            {
                CreateOutlookApp();
                dynamic accounts = olApp.Session.Accounts;
                List<string> acArray = new List<string>();
                foreach (dynamic account in accounts)
                {
                    acArray.Add(account.SmtpAddress);
                }
                return acArray;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                MailExtension.Dipose(olApp);
                olApp = null;
            }
        }
        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <returns></returns>
        public static bool SendMail(ParamSendMailModel paramSendMailModel)
        {
            try
            {
                CreateOutlookApp();
                mail = olApp.CreateItem(0);
                mail.To = paramSendMailModel.MailTo;
                mail.CC = paramSendMailModel.MailCC;
                mail.BCC = paramSendMailModel.MailBCC;
                mail.Subject = paramSendMailModel.MailSubject;
                mail.BodyFormat = 2;// Outlook.OlBodyFormat.olFormatHTML=2;//内容格式
                mail.HTMLBody = paramSendMailModel.MailHTMLBody;
                if (!string.IsNullOrEmpty(paramSendMailModel.MailAttachments))
                {
                    foreach (var item in paramSendMailModel.MailAttachments?.Split(';'))
                    {
                        if (!File.Exists(item))
                        {
                            throw new RPAException(eErrorMessageType.Error_NotExistMailAttachment);
                        }
                        mail.Attachments.Add(item);
                    }
                }
                dynamic accounts = olApp.Session.Accounts;
                if (accounts.Count > 0)
                {
                    dynamic account = olApp.Session.Accounts[paramSendMailModel.MailAccount];
                    if (account != null)
                    {
                        MailExtension.SetOutlookProperty(mail, "SendUsingAccount", account);
                    }
                }
                mail.Send();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                MailExtension.Dipose(mail);
                MailExtension.Dipose(olApp);
                mail = null;
                olApp = null;
            }
        }

        /// <summary>
        /// 回复邮件
        /// </summary>
        /// <returns></returns>
        public static bool ReplayMail(ParamReplyMailModel paramReplyMailModel)
        {
            try
            {
                CreateOutlookApp();
                mails = GetMailByFiter(olApp, paramReplyMailModel, true);
                if (mails.Count > 0)
                {
                    dynamic reply = null;
                    mail = mails.First();
                    if (paramReplyMailModel.MailReplyType == E_MailReplyModel.ReplySendTo)
                    {
                        reply = mail.Reply();
                    }
                    else
                    {
                        reply = mail.ReplyAll();
                    }
                    #region 引用原邮件内容----->按html拼接格式
                    string recipientsAddress = "";
                    foreach (dynamic recipient in mail.Recipients)
                    {
                        recipientsAddress = recipientsAddress + "," + recipient.Address;
                    }
                    StringBuilder replyContentWithOriginInfo = new StringBuilder();
                    replyContentWithOriginInfo.Append(paramReplyMailModel.ReplyMailHTMLBody);
                    replyContentWithOriginInfo.Append("<BR>");
                    replyContentWithOriginInfo.Append("<BR>");
                    //原文部分
                    replyContentWithOriginInfo.Append("<div style='border:none;border-top:solid #E1E1E1 1.0pt;padding:3.0pt 0cm 0cm 0cm'>");
                    replyContentWithOriginInfo.Append("<p class=MsoNormal>");
                    replyContentWithOriginInfo.Append($"<b><span style='font-size:11.0pt;font-family:等线'>发件人<span\r\nlang=EN-US>:</span></span></b><span lang=EN-US style='font-size:11.0pt;\r\nfont-family:等线'> {mail.SenderName} &lt;{mail.SenderEmailAddress}&gt; <br></span>");
                    replyContentWithOriginInfo.Append($"<b><span style='font-size:11.0pt;font-family:等线'>发送时间<span lang=EN-US>:</span></span></b><span\r\nlang=EN-US style='font-size:11.0pt;font-family:等线'> {mail.ReceivedTime.Year}</span><span\r\nstyle='font-size:11.0pt;font-family:等线'>年<span lang=EN-US> {mail.ReceivedTime.Month}</span>月<span\r\nlang=EN-US> {mail.ReceivedTime.Day}</span>日<span lang=EN-US>  {mail.ReceivedTime.Hour}:{mail.ReceivedTime.Minute}<br></span>");
                    replyContentWithOriginInfo.Append($"<b>收件人<span lang=EN-US>:</span></b><span lang=EN-US> {mail.ReceivedByName}\r\n&lt;{recipientsAddress.TrimStart(',')}&gt;<br></span>");
                    if (!string.IsNullOrEmpty(mail.CC))
                    {
                        replyContentWithOriginInfo.Append($"<b>抄送<span lang=EN-US>:</span></b><span lang=EN-US> {mail.CC}<br></span>");
                    }
                    replyContentWithOriginInfo.Append($"<b>主题<span lang=EN-US>:</span></b><span lang=EN-US> </span>{mail.Subject}<spanlang=EN-US><o:p></o:p></span></span>");
                    replyContentWithOriginInfo.Append("</p>");
                    replyContentWithOriginInfo.Append("</div>");
                    replyContentWithOriginInfo.Append("<p class=MsoNormal><span lang=EN-US><o:p>&nbsp;</o:p></span></p>");
                    replyContentWithOriginInfo.Append($"<p>{mail.HTMLBody}</p>");
                    #endregion
                    reply.BodyFormat = 2;// Outlook.OlBodyFormat.olFormatHTML=2;//内容格式
                    reply.HTMLBody = replyContentWithOriginInfo.ToString();

                    if (!string.IsNullOrEmpty(paramReplyMailModel.ReplyMailAttachments))
                    {
                        foreach (var item in paramReplyMailModel.ReplyMailAttachments?.Split(';'))
                        {
                            if (!File.Exists(item))
                            {
                                throw new RPAException(eErrorMessageType.Error_NotExistMailAttachment);
                            }
                            reply.Attachments.Add(item);
                        }
                    }
                    if (!string.IsNullOrEmpty(paramReplyMailModel.ReplyMailSubject))
                    {
                        reply.Subject = paramReplyMailModel.ReplyMailSubject;
                    }
                    reply.Send();
                    return true;
                }
                else
                {
                    //未找到需要处理的邮件
                    throw new RPAException(eErrorMessageType.Error_NotFoundMail);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                MailExtension.Dipose(mail);
                MailExtension.Dipose(mails);
                MailExtension.Dipose(olNameSpace);
                MailExtension.Dipose(olApp);
                mail = null;
                mails = null;
                olNameSpace = null;
                olApp = null;
            }
        }
        /// <summary>
        /// 查找邮件 待完善
        /// </summary>
        /// <returns></returns>
        public static List<MailInfo> CheckMail(ParamCheckMailModel paramCheckMailModel)
        {
            try
            {
                CreateOutlookApp();
                string folder = "";
                string filter = "";
                List<string> filters = new List<string>();
                bool isInstantSearchEnabled = olApp.Session.DefaultStore.IsInstantSearchEnabled;
                if (paramCheckMailModel.BoxFolderType == E_MailboxFolder.Inbox)
                {
                    folder = "inbox";
                }
                else if (paramCheckMailModel.BoxFolderType == E_MailboxFolder.Sented)
                {
                    folder = "sented";
                }
                else
                {
                    folder = paramCheckMailModel.BoxFolderName;
                }
                folderBox = MailExtension.GetFolderByFilter(olApp, paramCheckMailModel.MailAccount, paramCheckMailModel.BoxFolderType, paramCheckMailModel.BoxFolderName);
                if (folderBox == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundMailFolder);
                }

                //if (paramCheckMailModel.CheckScopeType == E_MailCheckScope.Read)
                //{
                //    filter = $"\"urn:schemas:httpmail:isread\" = true";
                //    filters.Add(filter);
                //}
                //if (paramCheckMailModel.CheckScopeType == E_MailCheckScope.UnRead)
                //{
                //    filter = $"\"urn:schemas:httpmail:isread\" = false";
                //    filters.Add(filter);
                //}
                if (!string.IsNullOrEmpty(paramCheckMailModel.MailToContains))
                {
                }
                if (!string.IsNullOrEmpty(paramCheckMailModel.SendToContains))
                {
                    if (isInstantSearchEnabled)
                    {
                        filter = $"\"http://schemas.microsoft.com/mapi/proptag/0x0065001F\" ci_phrasematch '{paramCheckMailModel.SendToContains}'";
                    }
                    else
                    {
                        filter = $"\"http://schemas.microsoft.com/mapi/proptag/0x0065001F\" LIKE '%{paramCheckMailModel.SendToContains}%'";
                    }
                    filters.Add(filter);
                }
                if (!string.IsNullOrEmpty(paramCheckMailModel.MailSubjectContains))
                {
                    if (isInstantSearchEnabled)
                    {
                        filter = $"\"urn:schemas:mailheader:subject\" ci_phrasematch '{paramCheckMailModel.MailSubjectContains}'";
                    }
                    else
                    {
                        filter = $"\"urn:schemas:mailheader:subject\" LIKE '%{paramCheckMailModel.MailSubjectContains}%'";
                    }
                    // 再添加精确匹配条件
                    string exactMatchFilter = $"\"urn:schemas:mailheader:subject\" = '{paramCheckMailModel.MailSubjectContains}'";
                    filter = $"({filter}) OR ({exactMatchFilter})";
                    filters.Add(filter);
                }
                if (!string.IsNullOrEmpty(paramCheckMailModel.MailHTMLBodyContains))
                {
                    if (isInstantSearchEnabled)
                    {
                        filter = $"\"urn:schemas:httpmail:textdescription\" ci_phrasematch '{paramCheckMailModel.MailHTMLBodyContains}'";
                    }
                    else
                    {
                        filter = $"\"urn:schemas:httpmail:textdescription\" LIKE '%{paramCheckMailModel.MailHTMLBodyContains}%'";
                    }
                    filters.Add(filter);
                }
                if (paramCheckMailModel.MailAttachmentType == E_MailAttachment.Exist)
                {
                    filter = $"\"urn:schemas:httpmail:hasattachment\" = true";
                    filters.Add(filter);
                }
                if (paramCheckMailModel.MailAttachmentType == E_MailAttachment.UnExist)
                {
                    filter = $"\"urn:schemas:httpmail:hasattachment\" = false";
                    filters.Add(filter);
                }
                // 获取收件箱中主题为【outlook测试】的邮件
                List<MailInfo> mailItems = new List<MailInfo>();
                //Outlook.MailItem mail = null;
                dynamic items = folderBox.Items.Restrict("[MessageClass] = 'IPM.Note'");
                if (paramCheckMailModel.CheckScopeType == E_MailCheckScope.Read)
                {
                    items = items.Restrict("[UnRead] = false");
                }
                if (paramCheckMailModel.CheckScopeType == E_MailCheckScope.UnRead)
                {
                    items = items.Restrict("[UnRead] = true");
                }
                if (paramCheckMailModel.MailDateList != null && paramCheckMailModel.MailDateList.Count > 1)
                {
                    if (paramCheckMailModel.MailDateList[0] != "")
                    {
                        if (DateTime.TryParse(paramCheckMailModel.MailDateList[0], out DateTime startDate))
                        {
                            items = items.Restrict($"[ReceivedTime] >= '{startDate.ToString("MM/dd/yyyy HH:mm")}'");
                        }
                        else
                        {
                            throw new Exception("Start Date Format Exception");
                        }
                    }
                    if (paramCheckMailModel.MailDateList[1] != "")
                    {
                        if (DateTime.TryParse(paramCheckMailModel.MailDateList[1], out DateTime sendDate))
                        {
                            items = items.Restrict($"[ReceivedTime] < '{sendDate.ToString("MM/dd/yyyy HH:mm")}'");
                        }
                        else
                        {
                            throw new Exception("End Date Format Exception");
                        }
                    }
                }
                if (filters.Count > 0)
                {
                    string filtsStr = "@SQL=";
                    foreach (var fil in filters)
                    {
                        if (filtsStr == "@SQL=")
                        {
                            filtsStr += $"{fil}";
                        }
                        else
                        {
                            filtsStr += $" and {fil}";
                        }
                    }
                    items = items.Restrict(filtsStr);
                }
                //按收件时间进行倒序排序
                items.Sort("[ReceivedTime]", true);
                foreach (object item in items)
                {
                    if (paramCheckMailModel.TopCount <= mailItems.Count && mailItems.Count > 0)
                    {
                        break;
                    }
                    List<MailAttachment> mailAttachments = new List<MailAttachment>();
                    //if (item is Outlook.MailItem)
                    //{
                    // mail = item as Outlook.MailItem;
                    mail = item;
                    if (!string.IsNullOrEmpty(paramCheckMailModel.MailToContains))
                    {
                        string recipientsName = "";
                        foreach (dynamic recipient in mail.Recipients)
                        {
                            recipientsName = recipientsName + "," + recipient.Address;
                        }
                        if (!recipientsName.Contains(paramCheckMailModel.MailToContains))
                        {
                            mail = null;
                            continue;
                        }
                    }
                    if (paramCheckMailModel.MailAttachmentType == E_MailAttachment.Exist)
                    {
                        if (mail.Attachments.Count < 1)
                        {
                            mail = null;
                            continue;
                        }
                    }
                    if (paramCheckMailModel.MailAttachmentType == E_MailAttachment.UnExist)
                    {
                        if (mail.Attachments.Count > 0)
                        {
                            mail = null;
                            continue;
                        }
                    }
                    foreach (dynamic attachment in mail.Attachments)
                    {
                        MailAttachment mailAttachment = new MailAttachment
                        {
                            FileName = attachment.FileName,
                            Size = attachment.Size
                        };
                        // PathName 仅对普通文件附件(olByValue=1)/链接附件(olByReference=4)可用;
                        // 内嵌项(olEmbeddeditem=5)、OLE 对象(olOLE=6)访问会抛 0x80004005,置空跳过。
                        if (attachment.Type == 1 || attachment.Type == 4)
                        {
                            mailAttachment.PathName = attachment.PathName;
                        }
                        mailAttachments.Add(mailAttachment);
                    }
                    mailItems.Add(new MailInfo
                    {
                        Account = mail.SenderEmailAddress,
                        Attach = mailAttachments,
                        BCC = mail.BCC,
                        Body = mail.Body,
                        CC = mail.CC,
                        Filter = "",
                        Folder = folder,
                        RecvBy = mail.ReceivedByName,
                        RecvTime = mail.ReceivedTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        SendBy = mail.SenderName,
                        SendByEmail = mail.SenderEmailAddress,
                        Subject = mail.Subject,
                        EntryID = mail.EntryID

                    });
                    //}
                }
                return mailItems;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                MailExtension.Dipose(mail);
                MailExtension.Dipose(mails);
                MailExtension.Dipose(folderBox);
                MailExtension.Dipose(olApp);
                mail = null;
                mails = null;
                folderBox = null;
                olApp = null;
            }
        }
        /// <summary>
        /// 移动邮件
        /// </summary>
        /// <returns></returns>
        public static bool MoveMail(ParamOperateMailModel paramOperateMailModel)
        {
            try
            {
                CreateOutlookApp();
                mails = GetMailByFiter(olApp, paramOperateMailModel);
                if (mails.Count > 0)
                {
                    foreach (var mail in mails)
                    {
                        dynamic destinationFolder = MailExtension.GetFolderByFilter(olApp, paramOperateMailModel.MailAccount, paramOperateMailModel.BoxFolderType, paramOperateMailModel.BoxFolderName);
                        if (destinationFolder != null)
                        {
                            mail.Move(destinationFolder);
                        }
                        else
                        {
                            //未找到目标文件夹
                            //throw new RPAException(eErrorMessageType.Error_NotFoundMailDestinationFolder);
                            logger.LogWarning($"{paramOperateMailModel.BoxFolderName}:未找到符合条件的目标文件夹");
                        }
                    }
                    return true;
                }
                else
                {
                    //未找到需要处理的邮件
                    throw new RPAException(eErrorMessageType.Error_NotFoundMail);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                MailExtension.Dipose(mail);
                MailExtension.Dipose(mails);
                MailExtension.Dipose(folderBox);
                MailExtension.Dipose(olNameSpace);
                MailExtension.Dipose(olApp);
                mail = null;
                mails = null;
                folderBox = null;
                olNameSpace = null;
                olApp = null;
            }
        }
        /// <summary>
        /// 删除邮件
        /// </summary>
        /// <returns></returns>
        public static bool DeteteMail(ParamOperateMailModel paramOperateMailModel)
        {
            try
            {
                CreateOutlookApp();
                mails = GetMailByFiter(olApp, paramOperateMailModel);
                if (mails.Count > 0)
                {
                    foreach (var mail in mails)
                    {
                        mail.Delete();
                    }
                    return true;
                }
                else
                {
                    //未找到需要处理的邮件
                    throw new RPAException(eErrorMessageType.Error_NotFoundMail);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                MailExtension.Dipose(mail);
                MailExtension.Dipose(mails);
                MailExtension.Dipose(folderBox);
                MailExtension.Dipose(olNameSpace);
                MailExtension.Dipose(olApp);
                mail = null;
                mails = null;
                folderBox = null;
                olNameSpace = null;
                olApp = null;
            }
        }
        /// <summary>
        /// 标记邮件（已读/未读）
        /// </summary>
        /// <returns></returns>
        public static bool MarkMail(ParamOperateMailModel paramOperateMailModel)
        {
            try
            {
                CreateOutlookApp();
                mails = GetMailByFiter(olApp, paramOperateMailModel);
                if (mails.Count > 0)
                {
                    foreach (var mail in mails)
                    {
                        if (paramOperateMailModel.MailOperateType == E_MailOperateType.MarkAsRead)
                        {
                            mail.UnRead = false;
                        }
                        if (paramOperateMailModel.MailOperateType == E_MailOperateType.MarkAsUnRead)
                        {
                            mail.UnRead = true;
                        }
                        mail.Save();
                    }
                    return true;
                }
                else
                {
                    //未找到需要处理的邮件
                    throw new RPAException(eErrorMessageType.Error_NotFoundMail);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                MailExtension.Dipose(mail);
                MailExtension.Dipose(mails);
                MailExtension.Dipose(folderBox);
                MailExtension.Dipose(olNameSpace);
                MailExtension.Dipose(olApp);
                mail = null;
                mails = null;
                folderBox = null;
                olNameSpace = null;
                olApp = null;
            }
        }
        /// <summary>
        /// 下载邮件附件
        /// </summary>
        /// <returns></returns>
        public static bool DownLoadMailAttachment(ParamOperateMailModel paramOperateMailModel)
        {
            try
            {
                CreateOutlookApp();
                mails = GetMailByFiter(olApp, paramOperateMailModel);
                if (mails.Count > 0)
                {
                    foreach (var mail in mails)
                    {
                        // 检查邮件是否包含附件
                        if (mail.Attachments.Count > 0)
                        {
                            foreach (dynamic attachment in mail.Attachments)
                            {
                                // SaveAsFile 仅对普通文件附件(olByValue=1)/链接附件(olByReference=4)可用,
                                // 内嵌项(olEmbeddeditem=5)、OLE 对象(olOLE=6)调用会抛 0x80004005,跳过。
                                if (attachment.Type != 1 && attachment.Type != 4)
                                {
                                    logger.LogWarning($"{mail.Subject}:跳过内嵌/OLE 附件 {attachment.FileName},无法保存");
                                    continue;
                                }
                                string savePath = Path.Combine(paramOperateMailModel.AttachmentsSaveAsFolder, attachment.FileName);
                                attachment.SaveAsFile(savePath);
                            }
                        }
                        else
                        {
                            //不存在需要保存的附件
                            //throw new RPAException(eErrorMessageType.Error_NotFoundMailAttachment);
                            logger.LogWarning($"{mail.Subject}:未找到符合条件的附件");
                        }
                    }
                    return true;
                }
                else
                {
                    //未找到需要处理的邮件
                    throw new RPAException(eErrorMessageType.Error_NotFoundMail);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                MailExtension.Dipose(mail);
                MailExtension.Dipose(mails);
                MailExtension.Dipose(folderBox);
                MailExtension.Dipose(olNameSpace);
                MailExtension.Dipose(olApp);
                mail = null;
                mails = null;
                folderBox = null;
                olNameSpace = null;
                olApp = null;
            }
        }

        /// <summary>
        /// 根据条件获取邮件对象
        /// </summary>
        /// <param name="olApp"></param>
        /// <param name="mailModel"></param>
        /// <param name="isOnlyFirst">是否只返回第一封</param>
        /// <returns></returns>
        public static List<dynamic> GetMailByFiter(dynamic olApp, ParamModel mailModel, bool isOnlyFirst = false)
        {
            string mailSubject = "";
            string recvBy = "";
            string sendBy = "";
            string filter = string.Empty;
            List<dynamic> mailItems = new List<dynamic>();
            if (mailModel.MailType == 0)
            {
                mailSubject = mailModel.AwaitingOperateMail;
                folderBox = MailExtension.GetFolderByFilter(olApp, mailModel.MailAccount, E_MailboxFolder.Inbox, "");
                filter = string.Format("[Subject] = '{0}'", mailSubject);
                dynamic items = folderBox.Items.Restrict($"[MessageClass] = 'IPM.Note' and {filter}");
                if (isOnlyFirst)
                {
                    if (items != null)
                    {
                        items.Sort("[ReceivedTime]", true);
                        mailItems.Add(items[0]);
                    }
                }
                else
                {
                    foreach (dynamic item in items)
                    {
                        mailItems.Add(item);
                    }
                }
            }
            else
            {
                olNameSpace = olApp.GetNamespace("MAPI");
                List<MailInfo> mailInfos = mailModel.AwaitingOperateMail.ToObject<List<MailInfo>>();
                if (mailInfos == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundMail);
                }
                mailInfos = mailInfos.OrderByDescending(a => a.RecvTime).ToList();
                foreach (MailInfo mailInfo in mailInfos)
                {
                    if (isOnlyFirst && mailItems.Count > 0)
                    {
                        break;
                    }
                    dynamic mail = null;
                    if (!string.IsNullOrEmpty(mailInfo.EntryID))
                    {
                        logger.LogInformation($"使用EntryID【{mailInfo.EntryID}】作为查找条件");
                        //mail = olNameSpace.GetItemFromID(mailInfo.EntryID) as Outlook.MailItem;
                        mail = olNameSpace.GetItemFromID(mailInfo.EntryID);
                        if (mail != null)
                        {
                            mailItems.Add(mail);
                            continue;
                        }
                    }
                    if (string.IsNullOrEmpty(mailInfo.EntryID) || mail == null)
                    {
                        mailSubject = mailInfo.Subject;
                        recvBy = mailInfo.RecvBy;
                        sendBy = mailInfo.SendByEmail;
                        filter = string.Empty;
                        List<string> filters = new List<string>();
                        bool isHasAttach = mailInfo.Attach.Count > 0 ? true : false;
                        if (!string.IsNullOrEmpty(mailSubject))
                        {
                            if (string.IsNullOrEmpty(filter))
                            {
                                filter = $" [Subject] = '{mailSubject}'";
                            }
                            else
                            {
                                filter = filter + $" and [Subject] = '{mailSubject}'";
                            }
                        }
                        if (!string.IsNullOrEmpty(recvBy))
                        {
                            if (string.IsNullOrEmpty(filter))
                            {
                                filter = $" [ReceivedByName] = '{recvBy}'";
                            }
                            else
                            {
                                filter = filter + $" and [ReceivedByName] = '{recvBy}'";
                            }
                        }
                        if (!string.IsNullOrEmpty(sendBy))
                        {
                            if (string.IsNullOrEmpty(filter))
                            {
                                filter = $" [SenderName] = '{sendBy}'";
                            }
                            else
                            {
                                filter = filter + $" and [SenderName] = '{sendBy}'";
                            }
                        }
                        if (mailInfo.Folder == "inbox")
                        {
                            folderBox = MailExtension.GetFolderByFilter(olApp, mailModel.MailAccount, E_MailboxFolder.Inbox, "");
                        }
                        else if (mailInfo.Folder == "sented")
                        {
                            folderBox = MailExtension.GetFolderByFilter(olApp, mailModel.MailAccount, E_MailboxFolder.Sented, "");
                        }
                        else
                        {
                            folderBox = MailExtension.GetFolderByFilter(olApp, mailModel.MailAccount, E_MailboxFolder.Other, mailInfo.Folder);
                            if (folderBox == null)
                            {
                                //throw new RPAException(eErrorMessageType.Error_NotFoundMailFolder);
                                logger.LogWarning($"{mailModel.MailAccount}:{mailInfo.Folder}未找到对应的文件夹");
                                continue;
                            }
                        }
                        dynamic items = folderBox.Items.Restrict("[MessageClass] = 'IPM.Note'").Restrict(filter);
                        // Outlook.MailItem mail = null;
                        foreach (object item in items)
                        {
                            mail = item;
                            //刷选收件时间差值在两分钟之内的，怕存在误差
                            if (Math.Abs((mail.ReceivedTime - Convert.ToDateTime(mailInfo.RecvTime)).TotalMinutes) > 2)
                            {
                                mail = null;
                                continue;
                            }
                            if (isHasAttach)
                            {
                                if (mail.Attachments.Count < 1)
                                {
                                    mail = null;
                                    continue;
                                }
                            }
                            else
                            {
                                if (mail.Attachments.Count > 0)
                                {
                                    mail = null;
                                    continue;
                                }
                            }
                            mailItems.Add(mail);
                            break;
                        }
                    }
                }
            }
            return mailItems;
        }

    }
}
