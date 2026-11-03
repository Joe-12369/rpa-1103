using II.RPA.EventMonitor.JSON;
using II.RPA.EventMonitor.Mail;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Pop3;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.EventMonitor
{
    class MailTrigger : Trigger, IDisposable
    {
        /// <summary>
        /// 取消时间
        /// </summary>
        private static readonly int CancelTokenMillSecends = 30000;
        #region 私有字段 属性
        /// <summary>
        /// 检测间隔（分钟）
        /// </summary>
        private int Interval { get; set; }
        private eConditionType Condition { get; set; }

        /// <summary>
        /// 协议，Pop3  or IMAP
        /// </summary>
        private string Protocol { get; set; }
        /// <summary>
        /// 服务器地址
        /// </summary>
        private string ServerAddress { get; set; }
        /// <summary>
        /// 端口
        /// </summary>
        private int Port { get; set; }
        /// <summary>
        /// 是否使用SSL
        /// </summary>
        private bool IsSSL { get; set; }
        /// <summary>
        /// 邮箱账号
        /// </summary>
        private string Account { get; set; }
        /// <summary>
        /// 登录密码
        /// </summary>
        private string Password { get; set; }
        /// <summary>
        /// 发件人地址包含的内容
        /// </summary>
        private string SendAddrContent { get; set; }
        /// <summary>
        /// 收件人地址包含的内容
        /// </summary>
        private string RecAddrContent { get; set; }
        /// <summary>
        /// 主题包含的内容
        /// </summary>
        private string ThemeContent { get; set; }
        /// <summary>
        /// 正文所包含的内容
        /// </summary>
        private string BodyContent { get; set; }
        /// <summary>
        /// 最后一个拉取到的邮件的时间
        /// </summary>
        private DateTime LastMailTime { get; set; }
        /// <summary>
        /// 最后一个拉取邮件的时间
        /// </summary>
        private List<UniqueId> ImapMailUidList { get; set; }
        /// <summary>
        /// 邮箱客户端
        /// </summary>
        private object MailClient = null;
        /// <summary>
        /// 监控是否停止 默认 True
        /// </summary>
        private bool monitorIsStop = true;
        private AutoResetEvent monitorWait = new AutoResetEvent(false);
        /// <summary>
        /// 监控线程停止的标志，如果未停止 则继续监控，返回true
        /// </summary>
        private bool MonitorThreadIsStop
        {
            get
            {
                return monitorIsStop;
            }
        }
        /// <summary>
        /// 监控线程
        /// </summary>
        private Thread monitorThread = null;
        /// <summary>
        /// 检空条件的数量（有效数量）
        /// </summary>
        private int conditionCount = 0;

        /// <summary>
        /// 邮件超时次数
        /// </summary>
        private int timeoutCount = 0;

        /// <summary>
        /// 连接异常次数Error
        /// </summary>
        private int connectErrorCount = 0;
        #endregion


        internal MailTrigger(MailTriggerJson json)
        {
            monitorThread = new Thread(MonitorMail);
            this.TriggerID = json.id;
            this.TriggerName = json.name;
            this.Interval = json.interval;
            this.Condition = (eConditionType)json.condition;
            this.Protocol = json.protocol;
            this.ServerAddress = json.serverAddress;
            this.Port = json.port;
            this.IsSSL = json.isSSL == 0 ? false : true;
            this.Account = json.email;
            this.Password = json.password;
            this.SendAddrContent = json.sendAddrContent;
            this.RecAddrContent = json.recAddrContent;
            this.ThemeContent = json.themeContent;
            this.BodyContent = json.bodyContent;
        }
        internal override void StartTrigger()
        {
            this.Status = eTriggerStatus.Starting;
            try
            {
                LastMailTime = DateTime.Now;
                monitorIsStop = false;
                if (monitorThread != null)
                {
                    monitorThread.IsBackground = true;
                    monitorThread.Start();
                }
                this.Status = eTriggerStatus.Runing;
            }
            catch (Exception ex)
            {
                string msg = "邮件触发器-启动异常";
                ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, ex);
            }

        }
        internal override void Continue()
        {
            this.HasTriggered = false;
            monitorWait.Set();
            this.Status = eTriggerStatus.Runing;
        }
        internal override void StopTrigger()
        {
            try
            {
                this.Status = eTriggerStatus.Stoping;
                this.monitorIsStop = true;
                monitorWait.Set();
                //try
                //{

                //    ThreadState tState = ThreadState.Stopped | ThreadState.StopRequested | ThreadState.Aborted | ThreadState.AbortRequested;
                //    if ((monitorThread.ThreadState & tState) == 0)
                //        monitorThread.Abort();
                //}
                //catch
                //{

                //}
                this.Dispose();
                this.Status = eTriggerStatus.Stoped;
            }
            catch (Exception ex)
            {
                string msg = "";
                SendMessage(eMessageStatus.Error_CloseTrigger_Mail, msg, ex);
            }

        }

        private void MonitorMail()
        {
            try
            {
                Loggers.Info("开始检测邮件信息");
                SetConditionCount();
                while (!this.MonitorThreadIsStop)
                {
                    if (this.HasTriggered)
                    {
                        monitorWait.WaitOne();
                        if (this.HasTriggered || this.MonitorThreadIsStop)
                        {
                            return;
                        }
                    }
                    DoMailMonitor();
                    Thread.Sleep(Interval * 60 * 1000);
                    //Thread.Sleep(Interval * 1000);
                }
            }
            catch (Exception ex)
            {
                string msg = "邮件触发器铺——执行异常";
                ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, ex);
            }
        }

        #region 获取具体邮件
        /// <summary>
        /// 执行邮件监控
        /// </summary>
        private void DoMailMonitor()
        {
            switch (this.Protocol.ToLower())
            {
                case "imap":
                    ReceiveEmailByImap();
                    break;
                case "pop3":
                default:
                    ReceiveEmailByPop3();
                    break;
            }
        }

        /// <summary>
        /// 设置监控有效条件的数量
        /// </summary>
        /// <returns></returns>
        private int SetConditionCount()
        {
            int count = 0;
            if (!string.IsNullOrEmpty(this.SendAddrContent))
            {
                count++;
            }
            if (!string.IsNullOrEmpty(this.RecAddrContent))
            {
                count++;
            }
            if (!string.IsNullOrEmpty(this.ThemeContent))
            {
                count++;
            }
            if (!string.IsNullOrEmpty(this.BodyContent))
            {
                count++;
            }
            this.conditionCount = count;
            return count;
        }

        /// <summary>
        /// 下载邮件内容 IMAP
        /// </summary>
        private void ReceiveEmailByImap()
        {
            try
            {
                var tokenSource = new CancellationTokenSource(10 * 1000);//10s超时
                //using (ImapClient client = new ImapClient())
                //{
                ImapClient client = null;
                #region 初始化客户端
                if (this.MailClient != null)
                {
                    client = (ImapClient)this.MailClient;
                }
                //else
                //{
                try
                {
                    if (client == null || !client.IsConnected || !client.IsAuthenticated)
                    {
                        try
                        {
                            if (client != null && client.IsConnected)
                            {
                                client.Disconnect(true);
                                client.Dispose();
                            }
                        }
                        catch
                        {

                        }
                        client = new ImapClient();
                        client.CheckCertificateRevocation = false;
                        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                        SecureSocketOptions secureSocketOptions = this.IsSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto;
                        client.Connect(this.ServerAddress, this.Port, secureSocketOptions, tokenSource.Token);
                        if (client.IsConnected)
                        {
                            client.AuthenticationMechanisms.Remove("XOAUTH2");
                            client.Authenticate(this.Account, this.Password, tokenSource.Token);
                            client.Identify();
                        }
                        else
                        {
                            string msg = ($"连接异常，检查服务器地址，也有可能给服务器策略拒绝");
                            ErrorAndStop(eMessageStatus.Error_Mail_ConfigError, msg, null);
                        }
                    }
                    if (client != null && client.IsConnected && client.IsAuthenticated)
                    {
                        this.MailClient = client;
                        connectErrorCount = 0;
                    }
                    else
                    {
                        connectErrorCount++;
                        string msg = ($"连接异常，检查服务器地址，也有可能给服务器策略拒绝,会在下次检测时重连,尝试重连3次");
                        Loggers.Warn(msg);
                        //ErrorAndStop(eMessageStatus.Error_Mail_ConfigError, msg, null);
                        //SendMessage(eMessageStatus.Error_Mail_ConfigError, msg, null);
                    }
                }
                catch (AuthenticationException ex)
                {
                    //connectErrorCount++;
                    string msg = ($"无效的用户名或密码:{0}---" + ex.Message);
                    ErrorAndStop(eMessageStatus.Error_Mail_ConfigError, msg, ex);
                }
                catch (ImapCommandException ex)
                {
                    //connectErrorCount++;
                    string msg = ($"尝试验证错误:{0}---" + ex.Message);
                    ErrorAndStop(eMessageStatus.Error_Mail_ConfigError, msg, ex);
                }
                catch (ImapProtocolException ex)
                {
                    //connectErrorCount++;
                    string msg = ($"尝试验证时的协议错误:{0}---" + ex.Message);
                    ErrorAndStop(eMessageStatus.Error_Mail_ConfigError, msg, ex);
                }
                catch (OperationCanceledException ex)
                {
                    //connectErrorCount++;
                    string msg = ($"邮箱身份操作超时:{0}---" + ex.Message);
                    timeoutCount++;
                    if (timeoutCount >= 3)
                    {
                        ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, ex);
                    }
                }
                catch (SocketException ex)
                {
                    string msg = ($"尝试验证时的协议错误:{0}---" + ex.Message);
                    ErrorAndStop(eMessageStatus.Error_Mail_Socket, msg, ex);
                }
                catch (Exception ex)
                {
                    //connectErrorCount++;
                    string msg = "";
                    ErrorAndStop(eMessageStatus.Error_Mail_ConfigError, msg, ex);
                }
                finally
                {
                    if (connectErrorCount >= 3)
                    {
                        string msg = ($"连接异常，检查服务器地址，也有可能给服务器策略拒绝");
                        ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, null);
                    }
                }
                //}
                #endregion
                if (this.Status != eTriggerStatus.Runing)
                {
                    return;
                }

                #region 邮件处理
                try
                {
                    tokenSource = new CancellationTokenSource(60 * 1000);//60s超时
                    if (!client.Inbox.IsOpen)
                        client.Inbox.Open(FolderAccess.ReadOnly, tokenSource.Token);
                    var query = SearchQuery.DeliveredAfter(this.LastMailTime);
                    MainForm.PrintMessage($"build query");
                    var uids = client.Inbox.Search(query, tokenSource.Token);
                    MainForm.PrintMessage($"Search uid count:{uids.Count}");
                    if (ImapMailUidList == null)
                    {
                        ImapMailUidList = new List<UniqueId>();
                    }
                    var newUids = uids.Except(ImapMailUidList, new UniqueIdComparer()).ToList();
                    MainForm.PrintMessage($"Search newUids count:{newUids.Count}");

                    if (newUids.Any())
                    {
                        MimeMessage message;
                        bool isMatched = false;
                        DateTime firstMail = this.LastMailTime;
                        for (int i = newUids.Count - 1; i >= 0; i--)
                        {
                            MainForm.PrintMessage($"GetMessage index :{i}");
                            message = client.Inbox.GetMessage(newUids[i], tokenSource.Token);
                            if (firstMail < message.Date.DateTime)
                            {
                                firstMail = message.Date.DateTime;
                            }
                            if (message.Date.DateTime <= LastMailTime)
                            {
                                MainForm.PrintMessage($"【Mail】[本次结束，已到邮件时间小于上一次最新邮件时间] LastMailTime:{LastMailTime},mailTime:{message.Date.DateTime}");
                                continue;
                            }
                            isMatched = IsMatchCondition(message);
                            if (isMatched)
                            {
                                MainForm.PrintMessage($"【Mail】[触发] {message.Date.Date}");
                                TryTrigger(this, null);
                                if (this.Status == eTriggerStatus.Suspend)
                                {
                                    break;
                                }
                            }
                        }
                        this.ImapMailUidList.AddRange(newUids);
                        this.LastMailTime = firstMail;
                        MainForm.PrintMessage($"本次检测结束");
                    }
                    else
                    {
                        MainForm.PrintMessage($"[IMAP]判断UID无最新,邮件总数：{uids.Count}");
                        //MainForm.PrintMessage("判断UID无最新");
                    }
                    if (client.Inbox.IsOpen)
                        client.Inbox.Close(cancellationToken: tokenSource.Token);
                    timeoutCount = 0;
                }
                catch (OperationCanceledException ex)
                {
                    string msg = ($"邮箱操作超时:{0}---" + ex.Message);
                    timeoutCount++;
                    if (timeoutCount >= 3)
                    {
                        ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, ex);
                    }
                }
                catch (Exception ex)
                {
                    Loggers.Warn("可能发生了一次连接异常的情况，可能是超时，会在下一次检测发生中，重新检测");
                    timeoutCount++;
                    if (timeoutCount >= 3)
                    {
                        ErrorAndStop(eMessageStatus.Error_Mail_Exception, "", ex);
                    }
                }
                #endregion

                //if (client.IsConnected)
                //    client.Disconnect(true);
                //}
            }
            catch (Exception ex)
            {
                string msg = "";
                ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, ex);
            }
        }
        #region imap
        internal void IdleLoop(object state)
        {
            var idle = (IdleState)state;

            lock (idle.Client.SyncRoot)
            {
                // Note: since the IMAP server will drop the connection after 30 minutes, we must loop sending IDLE commands that
                // last ~29 minutes or until the user has requested that they do not want to IDLE anymore.
                //
                // For GMail, we use a 9 minute interval because they do not seem to keep the connection alive for more than ~10 minutes.
                while (!idle.IsCancellationRequested)
                {
                    // Note: Starting with .NET 4.5, you can make this simpler by using the CancellationTokenSource .ctor that
                    // takes a TimeSpan argument, thus eliminating the need to create a timer.
                    using (var timeout = new CancellationTokenSource())
                    {
                        using (var timer = new System.Timers.Timer(Interval * 60 * 1000))
                        {
                            // End the IDLE command when the timer expires.
                            timer.Elapsed += (sender, e) => timeout.Cancel();
                            timer.AutoReset = false;
                            timer.Enabled = true;

                            try
                            {
                                // We set the timeout source so that if the idle.DoneToken is cancelled, it can cancel the timeout
                                idle.SetTimeoutSource(timeout);

                                if (idle.Client.Capabilities.HasFlag(ImapCapabilities.Idle))
                                {
                                    // The Idle() method will not return until the timeout has elapsed or idle.CancellationToken is cancelled
                                    idle.Client.Idle(timeout.Token, idle.CancellationToken);
                                }
                                else
                                {
                                    // The IMAP server does not support IDLE, so send a NOOP command instead
                                    idle.Client.NoOp(idle.CancellationToken);

                                    // Wait for the timeout to elapse or the cancellation token to be cancelled
                                    WaitHandle.WaitAny(new[] { timeout.Token.WaitHandle, idle.CancellationToken.WaitHandle });
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                // This means that idle.CancellationToken was cancelled, not the DoneToken nor the timeout.
                                break;
                            }
                            catch (ImapProtocolException)
                            {
                                // The IMAP server sent garbage in a response and the ImapClient was unable to deal with it.
                                // This should never happen in practice, but it's probably still a good idea to handle it.
                                //
                                // Note: an ImapProtocolException almost always results in the ImapClient getting disconnected.
                                break;
                            }
                            catch (ImapCommandException)
                            {
                                // The IMAP server responded with "NO" or "BAD" to either the IDLE command or the NOOP command.
                                // This should never happen... but again, we're catching it for the sake of completeness.
                                break;
                            }
                            finally
                            {
                                // We're about to Dispose() the timeout source, so set it to null.
                                idle.SetTimeoutSource(null);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        private List<string> PopsUIDs = new List<string>();
        /// <summary>
        /// 下载邮件内容 Pop3
        /// </summary>
        private void ReceiveEmailByPop3()
        {
            try
            {
                var tokenSource = new CancellationTokenSource(30 * 1000);//30s超时
                //using (Pop3Client client = new Pop3Client())
                {

                    Pop3Client client = null;
                    if (this.MailClient != null)
                    {
                        client = (Pop3Client)this.MailClient;
                    }
                    #region 初始化客户端
                    try
                    {
                        if (client == null || !client.IsConnected || !client.IsAuthenticated)
                        {
                            try
                            {
                                if (client != null && client.IsConnected)
                                {
                                    client.Disconnect(true);
                                    client.Dispose();
                                }
                            }
                            catch
                            {

                            }
                            client = new Pop3Client();
                            client.CheckCertificateRevocation = false;
                            Loggers.Info("CheckCertificateRevocation=false");
                            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                            SecureSocketOptions secureSocketOptions = this.IsSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto;
                            client.Connect(this.ServerAddress, this.Port, secureSocketOptions, tokenSource.Token);
                            if (client.IsConnected)
                            {
                                if (client.Capabilities.HasFlag(Pop3Capabilities.User))
                                {
                                    client.Capabilities = Pop3Capabilities.User;
                                }
                                client.AuthenticationMechanisms.Remove("XOAUTH2");
                                client.Authenticate(this.Account, this.Password, tokenSource.Token);
                            }
                            else
                            {
                                string msg = ($"连接异常，检查服务器地址，也有可能给服务器策略拒绝");
                                ErrorAndStop(eMessageStatus.Error_Mail_ConfigError, msg, null);
                            }
                        }
                        if (client != null && client.IsConnected && client.IsAuthenticated)
                        {
                            this.MailClient = client;
                            connectErrorCount = 0;
                        }
                        else
                        {
                            connectErrorCount++;
                            string msg = ($"连接异常，检查服务器地址，也有可能给服务器策略拒绝,会在下次检测时重连");
                            Loggers.Warn(msg);
                            //ErrorAndStop(eMessageStatus.Error_Mail_ConfigError, msg, null);
                            //SendMessage(eMessageStatus.Error_Mail_ConfigError, msg, null);
                        }
                    }
                    catch (AuthenticationException ex)
                    {
                        string msg = ($"无效的用户名或密码:{0}---" + ex.Message);
                        ErrorAndStop(eMessageStatus.Error_Mail_ConfigError, msg, ex);
                    }
                    catch (Pop3CommandException ex)
                    {
                        string msg = ($"尝试验证错误:{0}---" + ex.Message);
                        ErrorAndStop(eMessageStatus.Error_Mail_ConfigError, msg, ex);
                    }
                    catch (Pop3ProtocolException ex)
                    {
                        string msg = ($"尝试验证时的协议错误:{0}---" + ex.Message);
                        ErrorAndStop(eMessageStatus.Error_Mail_ConfigError, msg, ex);
                    }
                    catch (OperationCanceledException ex)
                    {
                        string msg = ($"邮箱操作超时:{0}---" + ex.Message);
                        timeoutCount++;
                        if (timeoutCount >= 3)
                        {
                            ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, ex);
                        }
                    }
                    catch (SocketException ex)
                    {
                        string msg = ($"尝试验证时的协议错误:{0}---" + ex.Message);
                        ErrorAndStop(eMessageStatus.Error_Mail_Socket, msg, ex);
                    }
                    catch (Exception ex)
                    {
                        connectErrorCount++;
                        string msg = "";
                        ErrorAndStop(eMessageStatus.Error_Mail_ConfigError, msg, ex);
                    }
                    finally
                    {
                        if (connectErrorCount >= 3)
                        {
                            string msg = ($"连接异常，检查服务器地址，也有可能给服务器策略拒绝");
                            ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, null);
                        }
                    }
                    //}
                    #endregion

                    if (this.Status != eTriggerStatus.Runing)
                    {
                        return;
                    }

                    #region 邮件处理
                    try
                    {
                        List<string> uids = new List<string>();
                        tokenSource = new CancellationTokenSource(10 * 1000);//30s超时
                        if (client.SupportsUids)
                        {
                            uids = client.GetMessageUids(tokenSource.Token).ToList();
                        }

                        MimeMessage message;
                        bool isMatched = false;
                        if (!uids.All(this.PopsUIDs.Contains))
                        {
                            DateTime firstMail = this.LastMailTime;
                            for (int i = uids.Count - 1; i >= 0; i--)
                            {
                                try
                                {
                                    tokenSource = new CancellationTokenSource(60 * 1000);//30s超时
                                    message = client.GetMessage(i, tokenSource.Token);
                                    if (i == uids.Count - 1)
                                    {
                                        firstMail = message.Date.DateTime;
                                    }
                                    if (message.Date.DateTime <= LastMailTime)
                                    {
                                        MainForm.PrintMessage($"【Mail】[本次结束，已到邮件时间小于上一次最新邮件时间] LastPullMailTime:{LastMailTime},mailTime:{message.Date.DateTime}");
                                        break;
                                    }
                                    //获取匹配状态
                                    isMatched = IsMatchCondition(message);
                                    if (isMatched)
                                    {
                                        MainForm.PrintMessage($"【Mail】[触发] {message.Date.Date}");
                                        TryTrigger(this, null);
                                        if (this.Status == eTriggerStatus.Suspend)
                                        {
                                            break;//已经匹配停止继续匹配
                                        }
                                    }
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    Loggers.Warn("发生了一次在获取邮件信息时，索引超出范围的异常，通常不需要特别处理");
                                }
                            }
                            this.PopsUIDs = uids;
                            this.LastMailTime = firstMail;
                        }
                        else
                        {
                            //MainForm.PrintMessage($"邮件总数：{uids.Count}");
                            MainForm.PrintMessage($"[POP3]判断UID无最新,邮件总数：{uids.Count}");
                        }
                        timeoutCount = 0;
                    }
                    catch (OperationCanceledException ex)
                    {
                        string msg = ($"邮箱操作超时:{0}---" + ex.Message);
                        timeoutCount++;
                        if (timeoutCount >= 3)
                        {
                            ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, ex);
                        }
                    }
                    catch (Pop3ProtocolException ex)
                    {
                        Loggers.Warn("发生了一次连接异常的情况，可能是超时，会在下一次检测发生中，重新检测");
                        timeoutCount++;
                        if (timeoutCount >= 3)
                        {
                            ErrorAndStop(eMessageStatus.Error_Mail_Exception, "", ex);
                        }
                    }
                    #endregion
                    if (client.IsConnected)
                        client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                string msg = "";
                ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, ex);
            }
        }

        /// <summary>
        /// 条件匹配
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool IsMatchCondition(MimeMessage message)
        {
            try
            {

                int _count = 0;
                if (!string.IsNullOrEmpty(this.SendAddrContent))
                {
                    if (message.From.Mailboxes.Any(m => m.Address.IsContains(this.SendAddrContent)))
                    {
                        _count++;
                        if (this.Condition == eConditionType.OR)
                        {
                            this.HasTriggered = true;
                            return true;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(this.RecAddrContent))
                {
                    if (message.To.Mailboxes.Any(m => m.Address.IsContains(this.RecAddrContent)))
                    {
                        _count++;
                        if (this.Condition == eConditionType.OR)
                        {
                            this.HasTriggered = true;
                            return true;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(this.ThemeContent))
                {
                    if (message.Subject.IsContains(this.ThemeContent))
                    {
                        _count++;
                        if (this.Condition == eConditionType.OR)
                        {
                            this.HasTriggered = true;
                            return true;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(this.BodyContent))
                {
                    if ((message.TextBody != null && message.TextBody.IsContains(this.BodyContent)) || (message.HtmlBody != null && message.HtmlBody.IsContains(this.BodyContent)))
                    {
                        _count++;
                        if (this.Condition == eConditionType.OR)
                        {
                            this.HasTriggered = true;
                            return true;
                        }
                    }
                }
                if (_count < this.conditionCount)
                {
                    return false;
                }
                else
                {
                    this.HasTriggered = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                string msg = "";
                ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, ex);
                return false;
            }
        }
        #endregion

        public void Dispose()
        {
            try
            {
                if (this.MailClient is ImapClient)
                {
                    if (((ImapClient)this.MailClient).IsConnected)
                        ((ImapClient)this.MailClient).Disconnect(true);
                }
                else if (this.MailClient is Pop3Client)
                {
                    if (((Pop3Client)this.MailClient).IsConnected)
                        ((Pop3Client)this.MailClient).Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                Loggers.Error($"释放mail客户端资源异常{this.MailClient.GetType()}");
            }
            try
            {
                int count = 0;
                while ((monitorThread.ThreadState != ThreadState.Stopped) && (monitorThread.ThreadState != ThreadState.Aborted))
                {
                    count++;
                    Thread.Sleep(50);
                    if (count > 20)
                    {
                        break;
                    }
                }
                monitorThread = null;
            }
            catch (Exception ex)
            {
                string msg = "邮件触发器-释放资源异常";
                ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, ex);
            }


        }
    }
    class IdleState
    {
        readonly object mutex = new object();
        CancellationTokenSource timeout;

        /// <summary>
        /// Get the cancellation token.
        /// </summary>
        /// <remarks>
        /// <para>The cancellation token is the brute-force approach to cancelling the IDLE and/or NOOP command.</para>
        /// <para>Using the cancellation token will typically drop the connection to the server and so should
        /// not be used unless the client is in the process of shutting down or otherwise needs to
        /// immediately abort communication with the server.</para>
        /// </remarks>
        /// <value>The cancellation token.</value>
        public CancellationToken CancellationToken { get; private set; }

        /// <summary>
        /// Get the done token.
        /// </summary>
        /// <remarks>
        /// <para>The done token tells the <see cref="Program.IdleLoop"/> that the user has requested to end the loop.</para>
        /// <para>When the done token is cancelled, the <see cref="Program.IdleLoop"/> will gracefully come to an end by
        /// cancelling the timeout and then breaking out of the loop.</para>
        /// </remarks>
        /// <value>The done token.</value>
        public CancellationToken DoneToken { get; private set; }

        /// <summary>
        /// Get the IMAP client.
        /// </summary>
        /// <value>The IMAP client.</value>
        public ImapClient Client { get; private set; }

        /// <summary>
        /// Check whether or not either of the CancellationToken's have been cancelled.
        /// </summary>
        /// <value><c>true</c> if cancellation was requested; otherwise, <c>false</c>.</value>
        public bool IsCancellationRequested
        {
            get
            {
                return CancellationToken.IsCancellationRequested || DoneToken.IsCancellationRequested;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdleState"/> class.
        /// </summary>
        /// <param name="client">The IMAP client.</param>
        /// <param name="doneToken">The user-controlled 'done' token.</param>
        /// <param name="cancellationToken">The brute-force cancellation token.</param>
        public IdleState(ImapClient client, CancellationToken doneToken, CancellationToken cancellationToken = default(CancellationToken))
        {
            CancellationToken = cancellationToken;
            DoneToken = doneToken;
            Client = client;

            // When the user hits a key, end the current timeout as well
            doneToken.Register(CancelTimeout);
        }

        /// <summary>
        /// Cancel the timeout token source, forcing ImapClient.Idle() to gracefully exit.
        /// </summary>
        void CancelTimeout()
        {
            lock (mutex)
            {
                if (timeout != null)
                    timeout.Cancel();
            }
        }

        /// <summary>
        /// Set the timeout source.
        /// </summary>
        /// <param name="source">The timeout source.</param>
        public void SetTimeoutSource(CancellationTokenSource source)
        {
            lock (mutex)
            {
                timeout = source;

                if (timeout != null && IsCancellationRequested)
                    timeout.Cancel();
            }
        }
    }
    sealed class UniqueIdComparer : IEqualityComparer<UniqueId>
    {
        public bool Equals(UniqueId x, UniqueId y) => x.Id == y.Id;
        public int GetHashCode(UniqueId obj) => obj.Id.GetHashCode();
    }
}
