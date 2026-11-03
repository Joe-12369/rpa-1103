using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using Serilog;

namespace II.RPA.Logging.Configurations
{
    /// <summary>
    /// 使用一个可切换的Logger来实现配置信息自动重装载
    /// 代码参考：https://github.com/tagcode/serilog-settings-reloader
    /// </summary>
    public class SwitchableLogger : ILogger, IDisposable
    {
        static ILogger silent = new LoggerConfiguration().CreateLogger();
        static SwitchableLogger instance = new SwitchableLogger();

        public static SwitchableLogger Instance => instance;

        protected ILogger logger;
        protected Func<ILogger> loggerProvider;

        public ILogger Logger
        {
            get
            {
                var _logger = logger;

                if (_logger != null)
                {
                    return _logger;
                }

                var _loggerProvider = loggerProvider;

                if (_loggerProvider != null)
                {
                    _logger = _loggerProvider();

                    if (_logger != null)
                    {
                        return _logger;
                    }
                }

                return silent;
            }
            set => Set(value);
        }

        public SwitchableLogger()
        {
        }

        public SwitchableLogger(ILogger logger)
        {
            this.logger = logger ?? silent;
        }

        public SwitchableLogger(Func<ILogger> loggerProvider)
        {
            this.loggerProvider = loggerProvider;
            if (loggerProvider == null) this.logger = silent;
        }

        public SwitchableLogger Set(ILogger newLogger, bool disposePrev = false)
        {
            if (this.logger == newLogger) return this;
            var oldLogger = this.logger;
            this.logger = newLogger ?? silent;
            this.loggerProvider = null;
            if (disposePrev && oldLogger is IDisposable disp) disp.Dispose();
            return this;
        }

        public SwitchableLogger Set(Func<ILogger> newLoggerProvider, bool disposePrev = false)
        {
            var oldLogger = this.logger;
            if (newLoggerProvider == null)
            {
                this.logger = silent;
                this.loggerProvider = null;
            }
            else
            {
                this.loggerProvider = newLoggerProvider;
                this.logger = null;
            }
            if (disposePrev && oldLogger is IDisposable disp) disp.Dispose();
            return this;
        }

        public ILogger GetCurrentLogger()
        {
            var _logger = logger;

            if (_logger != null)
            {
                return _logger;
            }

            var _loggerProvider = loggerProvider;

            if (_loggerProvider != null)
            {
                _logger = _loggerProvider();

                if (_logger != null)
                {
                    return _logger;
                }
            }

            return silent;
        }

        public ILogger ForContext(ILogEventEnricher enricher)
            => enricher == null ? this : new SwitchableLogger(new LoggerDecorator(GetCurrentLogger, l => l.ForContext(enricher)).Get);

        public ILogger ForContext(IEnumerable<ILogEventEnricher> enrichers)
            => enrichers == null ? this : new SwitchableLogger(new LoggerDecorator(GetCurrentLogger, l => l.ForContext(enrichers)).Get);

        public ILogger ForContext(string propertyName, object value, bool destructureObjects = false)
            => new SwitchableLogger(new LoggerDecorator(GetCurrentLogger, l => l.ForContext(propertyName, value, destructureObjects)).Get);

        public ILogger ForContext<TSource>()
            => ForContext(typeof(TSource));

        public ILogger ForContext(Type source)
            => source == null ? this : ForContext("SourceContext", source.FullName);

        public void Write(LogEvent logEvent)
            => Logger.Write(logEvent);

        public void Write(LogEventLevel level, string messageTemplate)
            => Logger.Write(level, messageTemplate);

        public void Write<T>(LogEventLevel level, string messageTemplate, T propertyValue)
            => Logger.Write<T>(level, messageTemplate, propertyValue);

        public void Write<T0, T1>(LogEventLevel level, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Write<T0, T1>(level, messageTemplate, propertyValue0, propertyValue1);

        public void Write<T0, T1, T2>(LogEventLevel level, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Write<T0, T1, T2>(level, messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Write(LogEventLevel level, string messageTemplate, params object[] propertyValues)
            => Logger.Write(level, messageTemplate, propertyValues);

        public void Write(LogEventLevel level, Exception exception, string messageTemplate)
            => Logger.Write(level, exception, messageTemplate);

        public void Write<T>(LogEventLevel level, Exception exception, string messageTemplate, T propertyValue)
            => Logger.Write<T>(level, exception, messageTemplate, propertyValue);

        public void Write<T0, T1>(LogEventLevel level, Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Write<T0, T1>(level, exception, messageTemplate, propertyValue0, propertyValue1);

        public void Write<T0, T1, T2>(LogEventLevel level, Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Write<T0, T1, T2>(level, exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Write(LogEventLevel level, Exception exception, string messageTemplate, params object[] propertyValues)
            => Logger.Write(level, exception, messageTemplate, propertyValues);

        public bool IsEnabled(LogEventLevel level)
            => Logger.IsEnabled(level);

        public void Verbose(string messageTemplate)
            => Logger.Verbose(messageTemplate);

        public void Verbose<T>(string messageTemplate, T propertyValue)
            => Logger.Verbose<T>(messageTemplate, propertyValue);

        public void Verbose<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Verbose<T0, T1>(messageTemplate, propertyValue0, propertyValue1);

        public void Verbose<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Verbose<T0, T1, T2>(messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Verbose(string messageTemplate, params object[] propertyValues)
            => Logger.Verbose(messageTemplate, propertyValues);

        public void Verbose(Exception exception, string messageTemplate)
            => Logger.Verbose(exception, messageTemplate);

        public void Verbose<T>(Exception exception, string messageTemplate, T propertyValue)
            => Logger.Verbose<T>(exception, messageTemplate, propertyValue);

        public void Verbose<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Verbose<T0, T1>(exception, messageTemplate, propertyValue0, propertyValue1);

        public void Verbose<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Verbose<T0, T1, T2>(exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Verbose(Exception exception, string messageTemplate, params object[] propertyValues)
            => Logger.Verbose(exception, messageTemplate, propertyValues);

        public void Debug(string messageTemplate)
            => Logger.Debug(messageTemplate);

        public void Debug<T>(string messageTemplate, T propertyValue)
            => Logger.Debug<T>(messageTemplate, propertyValue);

        public void Debug<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Debug<T0, T1>(messageTemplate, propertyValue0, propertyValue1);

        public void Debug<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Debug<T0, T1, T2>(messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Debug(string messageTemplate, params object[] propertyValues)
            => Logger.Debug(messageTemplate, propertyValues);

        public void Debug(Exception exception, string messageTemplate)
            => Logger.Debug(exception, messageTemplate);

        public void Debug<T>(Exception exception, string messageTemplate, T propertyValue)
            => Logger.Debug<T>(exception, messageTemplate, propertyValue);

        public void Debug<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Debug<T0, T1>(exception, messageTemplate, propertyValue0, propertyValue1);

        public void Debug<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Debug<T0, T1, T2>(exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Debug(Exception exception, string messageTemplate, params object[] propertyValues)
            => Logger.Debug(exception, messageTemplate, propertyValues);

        public void Information(string messageTemplate)
            => Logger.Information(messageTemplate);

        public void Information<T>(string messageTemplate, T propertyValue)
            => Logger.Information<T>(messageTemplate, propertyValue);

        public void Information<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Information<T0, T1>(messageTemplate, propertyValue0, propertyValue1);

        public void Information<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Information<T0, T1, T2>(messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Information(string messageTemplate, params object[] propertyValues)
            => Logger.Information(messageTemplate, propertyValues);

        public void Information(Exception exception, string messageTemplate)
            => Logger.Information(exception, messageTemplate);

        public void Information<T>(Exception exception, string messageTemplate, T propertyValue)
            => Logger.Information<T>(exception, messageTemplate, propertyValue);

        public void Information<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Information<T0, T1>(exception, messageTemplate, propertyValue0, propertyValue1);

        public void Information<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Information<T0, T1, T2>(exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Information(Exception exception, string messageTemplate, params object[] propertyValues)
            => Logger.Information(exception, messageTemplate, propertyValues);

        public void Warning(string messageTemplate)
            => Logger.Warning(messageTemplate);

        public void Warning<T>(string messageTemplate, T propertyValue)
            => Logger.Warning<T>(messageTemplate, propertyValue);

        public void Warning<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Warning<T0, T1>(messageTemplate, propertyValue0, propertyValue1);

        public void Warning<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Warning<T0, T1, T2>(messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Warning(string messageTemplate, params object[] propertyValues)
            => Logger.Warning(messageTemplate, propertyValues);

        public void Warning(Exception exception, string messageTemplate)
            => Logger.Warning(exception, messageTemplate);

        public void Warning<T>(Exception exception, string messageTemplate, T propertyValue)
            => Logger.Warning<T>(exception, messageTemplate, propertyValue);

        public void Warning<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Warning<T0, T1>(exception, messageTemplate, propertyValue0, propertyValue1);

        public void Warning<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Warning<T0, T1, T2>(exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Warning(Exception exception, string messageTemplate, params object[] propertyValues)
            => Logger.Warning(exception, messageTemplate, propertyValues);

        public void Error(string messageTemplate)
            => Logger.Error(messageTemplate);

        public void Error<T>(string messageTemplate, T propertyValue)
            => Logger.Error<T>(messageTemplate, propertyValue);

        public void Error<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Error<T0, T1>(messageTemplate, propertyValue0, propertyValue1);

        public void Error<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Error<T0, T1, T2>(messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Error(string messageTemplate, params object[] propertyValues)
            => Logger.Error(messageTemplate, propertyValues);

        public void Error(Exception exception, string messageTemplate)
            => Logger.Error(exception, messageTemplate);

        public void Error<T>(Exception exception, string messageTemplate, T propertyValue)
            => Logger.Error<T>(exception, messageTemplate, propertyValue);

        public void Error<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Error<T0, T1>(exception, messageTemplate, propertyValue0, propertyValue1);

        public void Error<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Error<T0, T1, T2>(exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Error(Exception exception, string messageTemplate, params object[] propertyValues)
            => Logger.Error(exception, messageTemplate, propertyValues);

        public void Fatal(string messageTemplate)
            => Logger.Fatal(messageTemplate);

        public void Fatal<T>(string messageTemplate, T propertyValue)
            => Logger.Fatal<T>(messageTemplate, propertyValue);

        public void Fatal<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Fatal<T0, T1>(messageTemplate, propertyValue0, propertyValue1);

        public void Fatal<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Fatal<T0, T1, T2>(messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Fatal(string messageTemplate, params object[] propertyValues)
            => Logger.Fatal(messageTemplate, propertyValues);

        public void Fatal(Exception exception, string messageTemplate)
            => Logger.Fatal(exception, messageTemplate);

        public void Fatal<T>(Exception exception, string messageTemplate, T propertyValue)
            => Logger.Fatal<T>(exception, messageTemplate, propertyValue);

        public void Fatal<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
            => Logger.Fatal<T0, T1>(exception, messageTemplate, propertyValue0, propertyValue1);

        public void Fatal<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
            => Logger.Fatal<T0, T1, T2>(exception, messageTemplate, propertyValue0, propertyValue1, propertyValue2);

        public void Fatal(Exception exception, string messageTemplate, params object[] propertyValues)
            => Logger.Fatal(exception, messageTemplate, propertyValues);

        public bool BindMessageTemplate(string messageTemplate, object[] propertyValues, out MessageTemplate parsedTemplate, out IEnumerable<LogEventProperty> boundProperties)
            => Logger.BindMessageTemplate(messageTemplate, propertyValues, out parsedTemplate, out boundProperties);

        public bool BindProperty(string propertyName, object value, bool destructureObjects, out LogEventProperty property)
            => Logger.BindProperty(propertyName, value, destructureObjects, out property);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing && logger is IDisposable disp) disp.Dispose();
        }

        public class LoggerDecorator
        {
            Func<ILogger> loggerProvider;
            Func<ILogger, ILogger> decorator;
            ILogger prevLogger;
            ILogger prevDecoratedLogger;

            public LoggerDecorator(Func<ILogger> loggerProvider, Func<ILogger, ILogger> decorator)
            {
                this.loggerProvider = loggerProvider ?? throw new ArgumentNullException(nameof(loggerProvider));
                this.decorator = decorator ?? throw new ArgumentNullException(nameof(decorator));
            }

            public ILogger Get()
            {
                ILogger currentLogger = loggerProvider() ?? SwitchableLogger.silent;

                if (currentLogger != prevLogger)
                {
                    prevDecoratedLogger = currentLogger == null ? null : decorator(currentLogger);
                    prevLogger = currentLogger;
                }

                return prevDecoratedLogger;
            }
        }
    }
}
