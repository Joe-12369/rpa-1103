using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace II.RPA.Core.Common
{
    /// <summary>
    /// 命令行参数解析类
    /// </summary>
    public static class CommandArgHelper
    {
        private static readonly CommandLineArgumentParser m_Arguments;
        private static readonly string[] commandArgs;
        static CommandArgHelper()
        {
            commandArgs = Environment.GetCommandLineArgs();
            m_Arguments = CommandLineArgumentParser.Parse(commandArgs);
        }
        public static string GetArgumentValue(string aKey)
        {
            if (m_Arguments != null)
            {
                var arg = m_Arguments.Get(aKey);
                return arg?.Next?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }
        public static string[] GetArgumentValues(string aKey, char aSplitChar)
        {
            var valueString = GetArgumentValue(aKey);
            if (string.IsNullOrWhiteSpace(valueString))
            {
                return Array.Empty<string>();
            }
            return valueString
                .Split(aSplitChar)
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray();
        }
        public static string GetArgumentValues(string aKey)
        {
            int startIndex = Array.IndexOf(commandArgs, aKey);
            if (startIndex < 0)
            {
                return string.Empty;
            }
            return string.Join(" ", commandArgs.Skip(startIndex + 1).Where(v => !string.IsNullOrWhiteSpace(v)));
        }
        public static string GetWithConfig(string aKey, string aConfigKey)
        {
            var value = GetArgumentValue(aKey);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            return ConfigurationManager.AppSettings[aConfigKey];
        }
    }
    internal class CommandLineArgument
    {
        private readonly List<CommandLineArgument> m_Arguments;
        private readonly int m_Index;
        private readonly string m_ArgumentText;

        internal CommandLineArgument Next
        {
            get
            {
                if (m_Index < m_Arguments.Count - 1)
                {
                    return m_Arguments[m_Index + 1];
                }
                return null;
            }
        }
        internal CommandLineArgument Previous
        {
            get
            {
                if (m_Index > 0)
                {
                    return m_Arguments[m_Index - 1];
                }
                return null;
            }
        }
        internal CommandLineArgument(List<CommandLineArgument> args, int index, string argument)
        {
            m_Arguments = args;
            m_Index = index;
            m_ArgumentText = argument;
        }

        internal CommandLineArgument Take()
        {
            return Next;
        }

        internal IEnumerable<CommandLineArgument> Take(int count)
        {
            var list = new List<CommandLineArgument>();
            var parent = this;
            for (int i = 0; i < count; i++)
            {
                var next = parent.Next;
                if (next == null)
                    break;
                list.Add(next);
                parent = next;
            }
            return list;
        }

        public static implicit operator string(CommandLineArgument argument)
        {
            return argument.m_ArgumentText;
        }

        public override string ToString()
        {
            return m_ArgumentText;
        }
    }
    public class CommandLineArgumentParser
    {

        internal readonly List<CommandLineArgument> m_Arguments;
        internal static CommandLineArgumentParser Parse(string[] args)
        {
            return new CommandLineArgumentParser(args);
        }

        internal CommandLineArgumentParser(string[] args)
        {
            m_Arguments = new List<CommandLineArgument>();
            for (int i = 0; i < args.Length; i++)
            {
                m_Arguments.Add(new CommandLineArgument(m_Arguments, i, args[i]));
            }
        }

        internal CommandLineArgument Get(string argumentName)
        {
            return m_Arguments.FirstOrDefault(p => p == argumentName);
        }

        internal bool Has(string argumentName)
        {
            return m_Arguments.Count(p => p == argumentName) > 0;
        }
    }
}
