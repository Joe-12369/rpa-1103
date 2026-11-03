namespace Test.II.RPA
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using Newtonsoft.Json.Linq;

    public class PluginLoaderService
    {
        private Assembly? _assembly;
        private object? _pluginLoaderObject;
        private Type? _pluginLoaderType;

        /// <summary>
        /// 加载并初始化插件
        /// </summary>
        /// <param name="baseDirectory">应用程序基础目录（通常是 AppDomain.CurrentDomain.BaseDirectory）</param>
        /// <returns>加载耗时（毫秒）</returns>
        public long LoadAndInitialize(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
                throw new ArgumentNullException(nameof(baseDirectory));

            Stopwatch sw = new();
            sw.Start();

            // 1. 清理 private 目录
            string standardPath = Path.Combine(Path.GetDirectoryName(baseDirectory), "plugins");
            string privatePath = Path.Combine(standardPath, "private");
            if (Directory.Exists(privatePath))
            {
                Directory.Delete(privatePath, true);
            }

            // 2. 加载插件程序集
            _assembly = Assembly.LoadFile(baseDirectory + "\\II.RPA.PluginLoader.dll");


            sw.Stop();
            long loadTime = sw.ElapsedMilliseconds;

            // 3. 创建 PluginLoader 实例
            _pluginLoaderObject = _assembly.CreateInstance("II.RPA.PluginLoader.PluginLoader");
            if (_pluginLoaderObject == null)
                throw new InvalidOperationException("无法创建 II.RPA.PluginLoader.PluginLoader 实例");

            _pluginLoaderType = _pluginLoaderObject.GetType();
            MethodInfo methodInit = _pluginLoaderType.GetMethod("Init");
            if (methodInit == null)
                throw new MissingMethodException("未找到 Init 方法");

            // 4. 执行 Init
            string pluginPath = standardPath;
            string packagesPath = pluginPath;
            methodInit.Invoke(_pluginLoaderObject, [pluginPath, packagesPath, @""]);

            // 5. 等待初始化完成
            int count = 0;
            while (count <= 200)
            {
                var isInitialized = _pluginLoaderType.GetProperty("IsInitialized");
                if (isInitialized == null) break;

                if (isInitialized.GetValue(_pluginLoaderObject).Equals(true))
                {
                    break;
                }
                Thread.Sleep(50);
                count++;
            }

            // 6. 设置共享变量
            MethodInfo methodSetSharedVariable = _pluginLoaderType.GetMethod("SetSharedVariable");
            if (methodSetSharedVariable != null)
            {
                JObject pJobj = new JObject
            {
                { "ZBrainServicePath", "http://127.0.0.1:6440" }
            };
                methodSetSharedVariable.Invoke(_pluginLoaderObject, new object[] { pJobj.ToString() });
            }

            return loadTime;
        }

        /// <summary>
        /// 获取 Run 方法，供外部调用
        /// </summary>
        public MethodInfo GetRunMethod()
        {
            return _pluginLoaderType?.GetMethod("Run")!;
        }

        /// <summary>
        /// 获取插件实例
        /// </summary>
        public object GetPluginInstance()
        {
            return _pluginLoaderObject!;
        }
    }

}


