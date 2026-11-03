using Grpc.Core;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using II.RPA.Logging.Configurations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Interception.InterceptionBehaviors;
using Unity.Interception.PolicyInjection.Pipeline;

namespace II.RPA.Core.GRPCService.Interception
{
    public class ServiceTraceBehavior : IInterceptionBehavior
    {
        private readonly ILogger m_Logger;
        public ServiceTraceBehavior(ILogger aLogger)
        {
            m_Logger = aLogger;
        }
        public IEnumerable<Type> GetRequiredInterfaces()
        {
            return Type.EmptyTypes;
        }
        /// <summary>
        /// Unity拦截器可以拦截流式通讯
        /// </summary>
        /// <param name="input"></param>
        /// <param name="getNext"></param>
        /// <returns></returns>
        public IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptionBehaviorDelegate getNext)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N");
            var method = input?.MethodBase?.Name ?? "unknown";
            object clientId = "null";
            try
            {
                if (input?.Inputs != null && input.Inputs.Count > 0)
                {
                    var args = input.Inputs[0];
                    clientId = args?.GetType().GetProperty("ClientId")?.GetValue(args) ?? "null";
                }
                m_Logger.LogCommunication(method, new StreamScope { RequestId = requestId, Client = $"{clientId}", Action = "Accept" });
                if (Global.IfCurrentLogLevelIsDebug)
                {
                    // Do not dump request bodies or headers: they can contain credentials.
                    TaskUtils.TraceThreadPool(method);
                }
            }
            catch (Exception ex)
            {
                LogFailure("diagnostics", ex);
            }

            IMethodReturn methodReturn;
            try
            {
                methodReturn = getNext().Invoke(input, getNext);
            }
            catch (Exception ex)
            {
                LogFailure("invoke", ex);
                throw;
            }

            if (methodReturn?.Exception != null)
            {
                LogFailure("method-return", methodReturn.Exception);
                LogCompletion("Error");
            }
            else if (methodReturn?.ReturnValue is Task returnTask)
            {
                _ = returnTask.ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        LogFailure("task", task.Exception);
                    }
                    LogCompletion(task.IsFaulted ? "Error" : task.IsCanceled ? "Canceled" : "Send");
                }, System.Threading.CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            else
            {
                LogCompletion("Send");
            }
            return methodReturn;

            void LogFailure(string stage, Exception exception)
            {
                try
                {
                    m_Logger.LogError($"ServiceTraceBehavior failed; method={method}; requestId={requestId}; clientId={clientId}; stage={stage}; elapsedMs={stopwatch.ElapsedMilliseconds}", exception);
                }
                catch
                {
                    // Diagnostics must not replace the original exception or interrupt the call.
                }
            }

            void LogCompletion(string action)
            {
                try
                {
                    m_Logger.LogCommunication($"{method}-{stopwatch.ElapsedMilliseconds}ms", new StreamScope { RequestId = requestId, Client = $"{clientId}", Action = action });
                }
                catch (Exception ex)
                {
                    LogFailure("completion-log", ex);
                }
            }
        }

        public bool WillExecute
        {
            get { return true; }
        }
    }
}
