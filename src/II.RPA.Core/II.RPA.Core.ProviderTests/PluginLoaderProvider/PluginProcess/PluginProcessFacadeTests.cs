using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using II.RPA.Core.IPCExtension.Message;
using II.RPA.Core.Log;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using Newtonsoft.Json;
using NUnit.Framework;

namespace II.RPA.Core.Provider.PluginLoaderProvider.PluginProcess.Tests
{
    [TestFixture()]
    public class PluginProcessFacadeTests
    {
        private PluginProcessFacade facade;

        [SetUp]
        public void PreStart()
        {
            if (this.facade != null)
            {
                System.Console.WriteLine($"Before Status:{this.facade.Status.ToString()}");
            }
        }

        [TearDown]
        public void PostStart()
        {
            if (this.facade != null)
            {
                System.Console.WriteLine($"After Status:{this.facade.Status.ToString()}");
            }
        }


        [OneTimeSetUp]
        public void SetUp()
        {
            var logger = new AdaptedLogger();
            this.facade = new PluginProcessFacade(logger, new MessageBroker());
            this.facade.PluginProcessSetupPath = @"D:\CodeWorkSpace\rpa5.9.New\ii.rpa\bin\II.RPA.PluginLoader\II.RPA.PluginProcess.exe";
        }

        [Test()]
        [Order(0)]
        public void PluginProcessFacadeTest()
        {
            Assert.IsTrue(this.facade.PluginProcess != null);
            Assert.IsTrue(this.facade.Status == PluginProcessStatus.New);
        }

        [Test()]
        [Order(1)]
        public void StartTest()
        {
            System.Console.WriteLine("PluginDLL Processor:" + this.facade.PluginProcessSetupPath);
            string pid = Process.GetCurrentProcess().Id.ToString();
            string startArgs = string.Format(@"-p {0} D:\CodeWorkSpace\rpa5.9.New\ii.rpa\bin\II.RPA.PluginLoader\II.RPA.PluginLoader.dll", pid);
            System.Console.WriteLine("start arg:" + startArgs);
            bool ret = this.facade.Start(startArgs);
            Assert.IsTrue(ret);
            System.Console.WriteLine("wait 5s to ready!");
            Thread.Sleep(5000);
        }

        [Test()]
        [Order(2)]
        public void InitTest()
        {
            PluginIPCInitializeRequest request = new PluginIPCInitializeRequest()
            {
                PackagesPath = @"D:\CodeWorkSpace\rpa5.9.New\ii.rpa\bin\plugins"
            };
            string ret = this.facade.Init(request).Result;
            System.Console.WriteLine(ret);
        }

        [Test()]
        [Order(3)]
        public void ShareCommonValueTest()
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            map.Add("key", Guid.NewGuid().ToString());

            PluginIPCShareValueRequest pc = new PluginIPCShareValueRequest()
            {
                JsonParam = JsonConvert.SerializeObject(map)
            };
            var msg = this.facade.ShareCommonValue(pc);
            System.Console.WriteLine(msg);
        }

        [Test()]
        [Order(4)]
        public void RunTest()
        {
            var data = new
            {
                filepath = @"D:\CodeWorkSpace\rpa5.9.New\ii.rpa\bin\II.RPA.Plugin\.screenshots\7f769a97-3023-49fc-bb66-8d69be66df72.png",
                isCVCapture = false,
                cvServer = "http://127.0.0.1:6440/cv_element_service_setup_detect",
                InspectStatus = 0,
                CaptureMode = 1,
                IsCvSupport = 1
            };
            PluginIPCRequest request = new PluginIPCRequest()
            {
                PluginName = "Element.MouseClick",
                Action = "Config",
                JsonParam = JsonConvert.SerializeObject(data)
            };
            string retValue = this.facade.Run(request);
            var msg = JsonConvert.DeserializeObject<JMessage>(retValue);
            System.Console.WriteLine(retValue);
            Assert.AreEqual(msg.Code, 0);
        }

        /// <summary>
        /// 保活测试
        /// </summary>
        [Test()]
        [Order(5)]
        public void KeepLiveTest()
        {
            int oldPid = this.facade.CurrentPluginProcessId;
            if (oldPid > 0)
            {
                try
                {
                    var foundItem = Process.GetProcessById(oldPid);
                    foundItem.Kill();
                    bool ret = foundItem.WaitForExit(2000);
                }
                catch
                {
                    Assert.Fail($"[{this.facade.CurrentPluginProcessId}]进程不存在");
                }
                Thread.Sleep(2000);
                Assert.AreNotEqual(this.facade.CurrentPluginProcessId, -1, "进程尚未复活");
                Assert.AreNotEqual(this.facade.CurrentPluginProcessId, oldPid, $"进程尚未复活old:{oldPid},new:{this.facade.CurrentPluginProcessId}");
            }
            else
            {
                Assert.Fail($"[{this.facade.CurrentPluginProcessId}]进程不存在");
            }
            System.Console.WriteLine($"Start new process:{this.facade.CurrentPluginProcessId},{this.facade.Status.ToString()}");
        }

        [OneTimeTearDown()]
        [Order(99)]
        public void StopProcessTest()
        {
            this.facade.StopProcess();
        }
    }
}