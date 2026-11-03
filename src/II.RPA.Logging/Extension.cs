using II.RPA.Logging.Configurations;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace II.RPA.Logging
{
    public static class Extension
    {
        public static string Limit(
            this string source,
            int? length = null,
            string mark = "内容已截断",
            bool includeSummary = true)
        {
            if (length == null)
            {
                var config = LogSettingsHelper.CurrentSettings.LevelString;

                if (true == config?.Equals(LogEventLevel.Debug.ToString(), System.StringComparison.OrdinalIgnoreCase))
                {
                    length = int.MaxValue;
                }
                else
                {
                    length = 1024;
                }
            }

            if (source == null || source.Length < length)
            {
                return source;
            }

            var lengthSummary = includeSummary ?
                $" {length}/{source.Length}" : null;

            var head = mark != null || includeSummary ?
                $"[{mark}{lengthSummary}]" : null;

            return $"{head} {source.Substring(0, length.Value)}...";
        }
    }
}