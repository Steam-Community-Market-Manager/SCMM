using Microsoft.Extensions.Logging;

namespace SCMM.Shared.Web
{
    public class HtmlLogger : ILoggerProvider, ILogger
    {
        private static readonly List<string> Logs = new List<string>();
        private const int MaxLogCount = 1000;

        public ILogger CreateLogger(string categoryName)
        {
            return this;
        }

        public void Dispose()
        {
            return;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var background = "inherit";
            var foreground = "inherit";
            var gray = "gray";
            switch (logLevel)
            {
                case LogLevel.Warning: background = "darkgoldenrod"; gray = "lightgray"; break;
                case LogLevel.Error: background = "darkred"; gray = "lightgray"; break;
                case LogLevel.Critical: background = "darkred"; break;
            }

            var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var log = $"<span style=\"color:{gray}\">[{timestamp}]</span> <strong><samp>{logLevel.ToString().ToUpper()}</samp></strong> <span>{formatter(state, exception)}</span><br/>";
            if (exception != null)
            {
                var innerException = exception;
                while (innerException != null)
                {
                    log += $"<span style=\"color:{gray}\">[{timestamp}]</span> <strong><samp>{exception.GetType().Name}</samp></strong><span> @ {innerException.Source ?? "Unknown"}:</span> <span>{innerException.Message}</span>";
                    if (!string.IsNullOrEmpty(innerException?.StackTrace))
                    {
                        log += $"<pre style=\"color: {gray}; margin:0px\">{innerException.StackTrace}</pre>";
                    }
                    else
                    {
                        log += $"<br/>";
                    }
                    innerException = innerException.InnerException;
                }
            }

            Logs.Add($"<div style=\"background-color:{background};color:{foreground}\">{log}</div>");
            var overcommited = (Logs.Count - MaxLogCount);
            if (overcommited > 0)
            {
                Logs.RemoveRange(0, overcommited);
            }
        }

        public static string Buffer
        {
            get
            {
                var inverseLogs = new List<string>(Logs);
                inverseLogs.Reverse();
                return string.Join('\n', inverseLogs);
            }
        }
    }

    public static class HtmlLoggerExtensions
    {
        public static ILoggingBuilder AddHtmlLogger(this ILoggingBuilder builder)
        {
            return builder.AddProvider(new HtmlLogger());
        }
    }
}
