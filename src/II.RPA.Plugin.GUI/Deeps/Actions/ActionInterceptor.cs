using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;

namespace II.RPA.Plugin.GUI.Deeps.Actions
{
    public class ActionInterceptor : IActionHandler
    {
        private readonly string _name;
        private readonly IActionHandler _inner;
        private static readonly ILogger _logger = Logger.OfContext();

        public ActionInterceptor(string name, IActionHandler inner)
        {
            _name = name;
            _inner = inner;
        }

        public string Execute(object input)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Handler {Name} started", _name);
                var result = _inner.Execute(input);
                sw.Stop();
                _logger.LogInformation("Handler {Name} executed in {Elapsed} ms", _name, sw.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "Handler {Name} failed after {Elapsed} ms", _name, sw.ElapsedMilliseconds);
                return "failed";
            }
        }
    }

}

