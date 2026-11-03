using II.RPA.Logging.Definitions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace II.RPA.Logging
{
    public static class CommonExtensions
    {
        /// <summary>
        /// 记录日志的时候收集上下文信息，方法名，文件路径，行号
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="loggerAction"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public static void WithContext(
            this ILogger logger,
            Action<ILogger> loggerAction,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
        {
            using (var scoep = logger.BeginScope(new
            {
                callerMemberName,
                callerFilePath,
                callerLineNumber
            }))
            {
                loggerAction(logger);
            }
        }

        /// <summary>
        /// 测试字符串，如果为null或者空，返回指定的字符串。有额外选项可用。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="replace"></param>
        /// <param name="replaceEmpty">为空是否替换，默认true</param>
        /// <param name="replaceWhiteSpace">为空白是否替换，默认false</param>
        /// <returns></returns>
        public static string NotNullOr(
            this string source,
            string replace,
            bool replaceEmpty = true,
            bool replaceWhiteSpace = false)
        {
            bool shouldReplace = source == null;

            shouldReplace = shouldReplace || (string.IsNullOrEmpty(source) && replaceEmpty);
            shouldReplace = shouldReplace || (string.IsNullOrWhiteSpace(source) && replaceWhiteSpace);

            if (shouldReplace)
            {
                return replace;
            }

            return source;
        }

        public static KeyValuePair<TKey, TValue> Tag<TKey, TValue>(this TValue value, TKey key)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }
}
