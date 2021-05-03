using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace SCMM.Shared.Web
{
    public class HtmlLogger : ILoggerProvider, ILogger
    {
        private static readonly List<string> Logs = new List<string>();
        private const int MaxLogCount = 300;

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

            var log = $"<span style=\"color:{gray}\">[{DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss")}]</span> <strong><samp>{logLevel.ToString().ToUpper()}</samp></strong> <span>{formatter(state, exception)}</span>";
            if (!String.IsNullOrEmpty(exception?.StackTrace))
            {
                log += $"<pre style=\"color: {gray}; margin:0px\">{exception.StackTrace}</pre>";
            }
            else
            {
                log += $"<br/>";
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
                return String.Join('\n', inverseLogs);
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
