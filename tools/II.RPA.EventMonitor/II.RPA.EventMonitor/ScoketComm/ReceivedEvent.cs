using II.RPA.EventMonitor.JSON;
using MailKit.Net.Imap;
using MailKit.Net.Pop3;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.EventMonitor
{
    public class ReceivedEvent
    {
        public static void ExecuteCommand(Socket connection, string requestInfo)
        {
            string commandType = "";
            string[] msgs = requestInfo.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            foreach (var content in msgs)
            {
                if (string.IsNullOrEmpty(content))
                    return;
                commandType = content.Substring(0, content.IndexOf(' '));
                var body = content.Substring(content.IndexOf(' ') + 1);
                switch (commandType)
                {
                    case "CloseTrigger":
                        CloseCommand(connection, body);
                        break;
                    case "KeepLive":
                        KeepLiveCommand(connection, body);
                        break;
                    case "StartTrigger":
                        StartCommand(connection, body);
                        break;
                    case "VerifyTriggerConfig":
                        VerifyTriggerConfigCommand(connection, body);
                        break;
                    default:
                        break;
                }
            }

        }
        public static void CloseCommand(Socket session, string requestInfo)
        {
            JMessage msg = new JMessage();
            //session.Logger.Debug($"Key:{requestInfo.Key},Body:{requestInfo.Body}");
            try
            {
                //List<string> appIDList = requestInfo.MessageBody.ToObject<List<string>>();
                List<string> appIDList = requestInfo.ToObject<List<string>>();
                foreach (var appid in appIDList)
                {
                    try
                    {
                        TriggerBus.CloseTrigger(appid);
                    }
                    catch (Exception ex)
                    {
                        Loggers.Error(ex);
                        msg.status = (int)eMessageStatus.Error_CloseTrigger;
                        msg.postBack.appId = appid;
                        session.TrySend(msg);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Loggers.Error(ex);
                msg.status = (int)eMessageStatus.Error_CloseTrigger;
                session.TrySend(msg);
                return;
            }

        }
        public static void KeepLiveCommand(Socket session, string requestInfo)
        {
            try
            {
                //session.Logger.Debug($"Key:{requestInfo.Key},Body:{requestInfo.Body}");
                MainForm.PrintMessage($"NewMessage:KeepLive {requestInfo}");
                int ret = requestInfo.ToInt32() + 1;
                session.TrySend(ret.ToString());
            }
            catch (Exception ex)
            {
                Loggers.Error($"Key:KeepLive,Body:{requestInfo}");
            }
        }
        public static void StartCommand(Socket session, string requestInfo)
        {
            JMessage msg = new JMessage();
            //session.Logger.Debug($"Key:{requestInfo.Key},Body:{requestInfo.Body}");
            TriggerJson jsonInput = null;
            try
            {
                jsonInput = requestInfo.ToObject<TriggerJson>();
                if (jsonInput == null)
                {
                    msg.status = (int)eMessageStatus.Error_StartTrigger_ParamsError;
                    msg.postBack = jsonInput.postBack;
                    session.TrySend(msg);
                    return;
                }
                jsonInput.SessionID = session.RemoteEndPoint.ToString();
                TriggerBus.StartTask(jsonInput);
            }
            catch (Exception ex)
            {
                Loggers.Error(ex);
                msg.status = (int)eMessageStatus.Error_StartTrigger_ParamsError;
                msg.postBack = jsonInput?.postBack;
                session.TrySend(msg);
                return;
            }

        }
        #region VerifyTriggerConfig
        private static PostBackJson PostBack { get; set; }
        public static void VerifyTriggerConfigCommand(Socket session, string requestInfo)
        {
            JMessage msg = new JMessage();
            //session.Logger.Debug($"Key:VerifyTriggerConfig,Body:{requestInfo}");
            TriggerJson jsonInput;
            try
            {
                jsonInput = requestInfo.ToObject<TriggerJson>();
                if (jsonInput == null)
                {
                    msg.status = (int)eMessageStatus.Error_StartTrigger_ParamsError;
                    session.TrySend(msg);
                    return;
                }
                ReceivedEvent.PostBack = jsonInput.postBack;
                if (jsonInput.mailTriggers.Count == 0)
                {
                    msg.status = (int)eMessageStatus.Error_Mail_ConfigError;
                    session.TrySend(msg);
                    return;
                }
                MailTriggerJson mailTrigger = jsonInput.mailTriggers.First();
                switch (mailTrigger.protocol.ToLower())
                {
                    case "imap":
                        ReceiveEmailByImap(mailTrigger, session);
                        break;
                    case "pop3":
                    default:
                        ReceiveEmailByPop3(mailTrigger, session);
                        break;
                }

            }
            catch (Exception ex)
            {
                Loggers.Error(ex);
                msg.status = (int)eMessageStatus.Error_StartTrigger_ParamsError;
                session.TrySend(msg);
                return;
            }
        }

        private static void ReceiveEmailByPop3(MailTriggerJson mailTrigger, Socket session)
        {
            string msg = "";
            JMessage message = new JMessage();
            var tokenSource = new CancellationTokenSource(10 * 1000);//10s超时
            #region 初始化客户端
            try
            {
                message.postBack = ReceivedEvent.PostBack;
                message.postBack.AddTargetTriggerID(mailTrigger.id);
                using (Pop3Client client = new Pop3Client())
                {
                    client.CheckCertificateRevocation = false;
                    Loggers.Info("CheckCertificateRevocation=false");
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    SecureSocketOptions secureSocketOptions = mailTrigger.isSSL == 1 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto;
                    client.Connect(mailTrigger.serverAddress, mailTrigger.port, secureSocketOptions, tokenSource.Token);
                    if (client.Capabilities.HasFlag(Pop3Capabilities.User))
                    {
                        client.Capabilities = Pop3Capabilities.User;
                    }
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.Authenticate(mailTrigger.email, mailTrigger.password, tokenSource.Token);
                    client.Disconnect(true);
                    message.status = (int)eMessageStatus.Success_Verify;
                    session.TrySend(message);
                    return;
                }
            }
            catch (AuthenticationException ex)
            {
                msg = ($"无效的用户名或密码");
                Loggers.Error(ex);
            }
            catch (Pop3CommandException ex)
            {
                msg = ($"尝试验证错误");
                Loggers.Error(ex);
            }
            catch (Pop3ProtocolException ex)
            {
                msg = ($"尝试验证时的协议错误");
                Loggers.Error(ex);
            }
            catch (TimeoutException ex)
            {
                msg = ($"邮箱操作超时");
                Loggers.Error(ex);
            }
            catch (OperationCanceledException ex)
            {
                msg = ($"邮箱操作超时");
                Loggers.Error(ex);
            }
            catch (SocketException ex)
            {
                msg = ($"邮箱服务器地址或端口错误");
                Loggers.Error(ex);
            }
            catch (Exception ex)
            {
                message.status = (int)eMessageStatus.Error_VerifyConfig;
                Loggers.Error(ex);
            }

            if (msg != null)
            {
                message.status = (int)eMessageStatus.Error_VerifyConfig_Mail;
                message.message = msg;
            }
            session.TrySend(message);
            #endregion

        }
        private static void ReceiveEmailByImap(MailTriggerJson mailTrigger, Socket session)
        {
            string msg = "";
            JMessage message = new JMessage();
            try
            {
                message.postBack = ReceivedEvent.PostBack;
                message.postBack.AddTargetTriggerID(mailTrigger.id);
                var tokenSource = new CancellationTokenSource(2 * 1000);//10s超时
                using (ImapClient client = new ImapClient())
                {
                    #region 初始化客户端
                    try
                    {
                        client.CheckCertificateRevocation = false;
                        Loggers.Info("CheckCertificateRevocation=false");
                        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                        SecureSocketOptions secureSocketOptions = mailTrigger.isSSL == 1 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto;
                        client.Connect(mailTrigger.serverAddress, mailTrigger.port, secureSocketOptions, tokenSource.Token);
                        client.Authenticate(mailTrigger.email, mailTrigger.password, tokenSource.Token);
                        client.Identify();
                        client.Disconnect(true);
                        message.status = (int)eMessageStatus.Success_Verify;
                        session.TrySend(message);
                        return;
                    }
                    catch (AuthenticationException ex)
                    {
                        msg = ($"无效的用户名或密码");
                        Loggers.Error(ex);
                    }
                    catch (ImapCommandException ex)
                    {
                        msg = ($"尝试验证错误");
                        Loggers.Error(ex);
                    }
                    catch (ImapProtocolException ex)
                    {
                        msg = ($"尝试验证时的协议错误");
                        Loggers.Error(ex);
                    }
                    catch (TimeoutException ex)
                    {
                        msg = ($"邮箱操作超时");
                        Loggers.Error(ex);
                    }
                    catch (OperationCanceledException ex)
                    {
                        msg = ($"邮箱操作超时");
                        Loggers.Error(ex);
                    }
                    catch (SocketException ex)
                    {
                        msg = ($"邮箱服务器地址或端口错误");
                        Loggers.Error(ex);
                    }
                    catch (Exception ex)
                    {
                        Loggers.Error(ex);
                        message.status = (int)eMessageStatus.Error_VerifyConfig;
                    }
                    #endregion
                }

            }
            catch (Exception ex)
            {
                Loggers.Error(ex);
                message.status = (int)eMessageStatus.Error_VerifyConfig;
            }
            if (msg != null)
            {
                message.status = (int)eMessageStatus.Error_VerifyConfig_Mail;
                message.message = msg;
            }
            session.TrySend(message);
        }
        #endregion
    }
}
