using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.CircuitPolicy.Progress
{
    public struct ProgressData
    {
        public Statistic[] Statistics { get; }
        public string[] Messages { get; }
        public ProgressData(Statistic[] statistics, IEnumerable<string> messages)
        {
            Statistics = statistics;
            Messages = messages.ToArray();
        }

        public ProgressData(Statistic[] statistics, string message)
        {
            Statistics = statistics;
            Messages = new[] { message };
        }
    }
    public struct Statistic
    {
        public string Description { get; }

        public double Value { get; }

        public Level Level { get; }

        public Statistic(string description, double value, Level level)
        {
            Description = description;
            Value = value;
            Level = level;
        }

        public Statistic(string description, double value) : this(description, value, Level.INFO)
        {

        }
    }
    public enum Level
    {
        INFO = 0,
        WARN = 1,
        ERROR = 2,
        FATAL = 3
    }
}
