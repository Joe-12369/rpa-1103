using Grpc.Core;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.GRPCService.Interception;
using II.RPA.Core.Log;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Rpa.Core.Mobile;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCService.Service
{
    public class MobileDeviceManagerGRPCService : MobileDeviceManagerService.MobileDeviceManagerServiceBase, IGRPCService
    {
        private readonly BlockingCollection<AsyncMessage<Message, Message>> m_SubMessageInfoQueue;
        private MobileDeviceManagerService.MobileDeviceManagerServiceClient m_ServiceClient;
        private GrpcClient m_Client;
        private Process m_Process;
        private readonly ILogger m_Logger;
        public MobileDeviceManagerGRPCService(ILogger aLogger)
        {
            m_Logger = aLogger;
            m_SubMessageInfoQueue = new BlockingCollection<AsyncMessage<Message, Message>>();
        }
        public override async Task<MobileResponse> CaptureMobileUnary(MobileRequest request, ServerCallContext context)
        {
            if (isExistMobileApp)
            {
                // 拾取需要等待用户点击或取消，不适用普通请求的默认 30 秒期限。
                return await m_ServiceClient.CaptureMobileUnaryAsync(
                    request, deadline: DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), cancellationToken: context.CancellationToken);
            }
            else
            {
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "手机管理界面未开启",
                    Info = "{}"
                });
            }
        }
        public override async Task<MobileResponse> CheckMobileUnary(MobileRequest request, ServerCallContext context)
        {
            if (isExistMobileApp)
            {
                return await m_ServiceClient.CheckMobileUnaryAsync(request);
            }
            else
            {
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "手机管理界面未开启",
                    Info = "{}"
                });
            }
        }
        public override Task<MobileResponse> GetConnectStatusUnary(MobileRequest request, ServerCallContext context) =>
            base.GetConnectStatusUnary(request, context);
        public override Task<MobileRequest> GetDeviceInfoUnary(MobileRequest request, ServerCallContext context) =>
            base.GetDeviceInfoUnary(request, context);
        public override async Task<MobileResponse> GetPluginStatusUnary(MobileRequest request, ServerCallContext context)
        {
            var mobileResponse = new MobileResponse();
            try
            {
                List<CheckResultInfo> retInfo = new List<CheckResultInfo>();
                //插件已安装
                if (this.IsInstallMobileTools())
                {
                    retInfo.Add(new CheckResultInfo { CheckName = "PluginIsInstall", CheckSuccess = true });
                    //存在连接电脑的手机
                    if (this.IsExistsIsOnMobiles())
                    {
                        retInfo.Add(new CheckResultInfo { CheckName = "MobileIsOn", CheckSuccess = true });
                    }
                    else
                    {
                        retInfo.Add(new CheckResultInfo { CheckName = "MobileIsOn", CheckSuccess = false });
                    }
                }
                else
                {
                    retInfo.Add(new CheckResultInfo { CheckName = "PluginIsInstall", CheckSuccess = false });
                    retInfo.Add(new CheckResultInfo { CheckName = "MobileIsOn", CheckSuccess = false });
                }
                mobileResponse.Code = "0";
                mobileResponse.Info = JsonConvert.SerializeObject(retInfo);
                m_Logger.LogInfo($"GetPluginStatusUnary:result【{mobileResponse.Info}】");
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"GetPluginStatusUnary:{ex}");
                mobileResponse.Code = "1";
            }
            return await Task.FromResult(mobileResponse);
        }
        public async override Task<MobileResponse> GetPointUnary(MobileRequest request, ServerCallContext context)
        {
            if (isExistMobileApp)
            {
                return await m_ServiceClient.GetPointUnaryAsync(request, deadline: DateTime.MaxValue, cancellationToken: context.CancellationToken);
            }
            else
            {
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "手机管理界面未开启",
                    Info = "{}"
                });
            }
        }
        public override Task<Message> PluginInstallResultToClintUnary(Message request, ServerCallContext context)
        {
            m_Logger.LogInfo($"PluginInstallResultToClintUnary:{request.MessageId};{request.Action}");
            AsyncMessage<Message, Message> asyncMessage = new AsyncMessage<Message, Message>(request.MessageId);
            asyncMessage.AddInput(request);
            if (m_SubMessageInfoQueue.TryAdd(asyncMessage))
            {
                return Task.FromResult(request);
            }
            else
            {
                throw new InvalidOperationException($"Fail to add message {request.MessageId}");
            }
        }
        public override async Task<MobileResponse> StartDeviceUnary(MobileRequest request, ServerCallContext context)
        {
            try
            {
                if (isExistMobileApp)
                {
                    m_Logger.LogInfo($"App is Exist ,Only Active MainWind");
                    return await m_ServiceClient.StartDeviceUnaryAsync(request);
                }
                else
                {
                    m_Logger.LogInfo($"StartDeviceUnary:{request.InParameter};{request.OperationType}");
                    var channelName = Guid.NewGuid().ToString();
                    m_Client = new GrpcClient(new ClientOptions
                    {
                        Endpoint = new NamedPipeEndpoint(channelName)
                    });
                    var actionType = request.OperationType;
                    var inParameter = request.InParameter;
                    m_ServiceClient = m_Client.CreateClient(invoker => new MobileDeviceManagerService.MobileDeviceManagerServiceClient(invoker));
                    m_Process?.Close();

                    var clintType = "1";
                    if (Global.ClientIDString.ToLower() == "z-factory")
                    {
                        clintType = "1";
                    }
                    if (Global.ClientIDString.ToLower() == "z-bot")
                    {
                        clintType = "2";
                    }
                    string paraStr = $"-c {clintType} -p {Process.GetCurrentProcess().Id} -n {channelName} -s {Global.ServerPort} -action {actionType} -para {inParameter}";
                    m_Logger.LogInfo($"StartDeviceUnary paraStr:{paraStr}");

                    Process process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        Arguments = paraStr,
                        FileName = "II.RPA.MobileDeviceManager.exe",
                        UseShellExecute = false,
                        CreateNoWindow = false
                    };
                    process.EnableRaisingEvents = true;
                    process.Exited += M_Process_Exited;
                    process.Start();

                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "0",
                        Message = "starting"
                    });
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"StartDeviceUnary Error:{ex}");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "打开手机管理界面异常",
                    Info = "{}"
                });
            }
        }
        public override async Task SubMessageInfoServerStreaming(Message request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                var asyncMessage = m_SubMessageInfoQueue.Take(context.CancellationToken);
                if (asyncMessage != null)
                {
                    m_Logger.LogInfo($"SubMessageInfoServerStreaming:{asyncMessage.Id}");
                    await responseStream.WriteAsync(asyncMessage.GetInput());
                }
            }
        }

        public override async Task<MobileResponse> CloseDeviceUnary(MobileRequest request, ServerCallContext context)
        {
            if (isExistMobileApp)
            {
                return await m_ServiceClient.CloseDeviceUnaryAsync(request);
            }
            else
            {
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "手机管理界面未开启",
                    Info = "{}"
                });
            }
        }
        public override async Task<MobileResponse> GetCurrentAppPackageNameUnary(MobileRequest request, ServerCallContext context)
        {
            if (isExistMobileApp)
            {
                return await m_ServiceClient.GetCurrentAppPackageNameUnaryAsync(request);
            }
            else
            {
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "手机管理界面未开启",
                    Info = "{}"
                });
            }
        }

        public override async Task<MobileResponse> CaptureMobileElementShotUnary(MobileRequest request, ServerCallContext context)
        {
            if (isExistMobileApp)
            {
                return await m_ServiceClient.CaptureMobileElementShotUnaryAsync(request, deadline: DateTime.MaxValue, cancellationToken: context.CancellationToken);
            }
            else
            {
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "手机管理界面未开启",
                    Info = "{}"
                });
            }
        }
        public override async Task<MobileResponse> CaptureDomainScopeUnary(MobileRequest request, ServerCallContext context)
        {
            if (isExistMobileApp)
            {
                return await m_ServiceClient.CaptureDomainScopeUnaryAsync(request, deadline: DateTime.MaxValue, cancellationToken: context.CancellationToken);
            }
            else
            {
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "手机管理界面未开启",
                    Info = "{}"
                });
            }
        }
        public override async Task<MobileResponse> CheckMobileImageUnary(MobileRequest request, ServerCallContext context)
        {
            if (isExistMobileApp)
            {
                return await m_ServiceClient.CheckMobileImageUnaryAsync(request);
            }
            else
            {
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "手机管理界面未开启",
                    Info = "{}"
                });
            }
        }

        public override async Task<MobileResponse> TestSlidUnary(MobileRequest request, ServerCallContext context)
        {
            if (isExistMobileApp)
            {
                return await m_ServiceClient.TestSlidUnaryAsync(request);
            }
            else
            {
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "手机管理界面未开启",
                    Info = "{}"
                });
            }
        }
        private void M_Process_Exited(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        #region 判断手机插件和当前电脑是否存在手机连接

        public static string adbPath
        {
            get
            {
                return System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA\\mobile\\AndroidSDK\\platform-tools\\adb.exe");
            }
        }
        public bool IsInstallMobileTools()
        {
            string androidSdkPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA\\mobile\\AndroidSDK\\platform-tools\\adb.exe");
            string appiumPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA\\mobile\\appium\\node.exe");
            string javaSdkPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA\\mobile\\JavaSDK");
            if (File.Exists(androidSdkPath) && File.Exists(appiumPath) && Directory.Exists(javaSdkPath))
            {
                return true;
            }
            return false;
        }
        public bool IsExistsIsOnMobiles()
        {
            List<string> obj = ExecuteCmd(adbPath + " devices").Replace("List of devices attached", "").Replace("\r\n\r\n", "")
                .Replace("\r\n", "&")
                .Replace("\t", "$")?.Split('&').ToList();

            foreach (string item in obj)
            {
                List<string> deviceitem = item?.Split('$').ToList();
                if (deviceitem.Count == 2)
                {
                    string udid = deviceitem[0];
                    if (!(deviceitem[1] == "offline") || !udid.Contains("62001"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public string ExecuteCmd(string command)
        {
            string output = "";
            if (command != null && !command.Equals(""))
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C " + command;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
                try
                {
                    if (process.Start())
                    {
                        process.WaitForExit(2000);
                        output = process.StandardOutput.ReadToEnd();
                    }
                }
                catch (Exception em)
                {
                    //Logging.Warn("Dos命令执行失败", em);
                }
                finally
                {
                    process?.Close();
                }
            }
            return output;
        }

        public class CheckResultInfo
        {
            public string CheckName { get; set; }
            public bool CheckSuccess { get; set; }
        }

        private bool isExistMobileApp
        {
            get { return ProcessHelper.ExistProcess("II.RPA.MobileDeviceManager"); }
        }
        #endregion

        public ServerServiceDefinition BindingToService()
        {
            return MobileDeviceManagerService.BindService(this);
        }
    }
}
