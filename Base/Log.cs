using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Heleus.Base
{
    public enum LogLevels
    {
        None,
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    public sealed class LogEvent
    {
        public LogLevels LogLevel;
        public string Message;
        public string OriginalMessage;

        public LogEvent(LogLevels logLevel, string message, string originalMessage)
        {
            LogLevel = logLevel;
            Message = message;
            OriginalMessage = originalMessage;
        }
    }

    public interface ILogger
    {
        string LogName { get; }
    }

    public static class Log
    {
        public static LogLevels LogLevel = LogLevels.Info;

        public static bool LogTrace => LogLevel <= LogLevels.Trace; // Can be used to avoid costly ToString and other operations
        public static bool LogDebug => LogLevel <= LogLevels.Debug;

        public static PubSub PubSub;

        public static bool ShowConsoleOutput = true;
        public static bool ShowSystemDiagnostics = false;

        static readonly HashSet<string> _ignores = new HashSet<string>();

        static Log()
        {
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                var exception = GetInnerException(e.ExceptionObject as Exception);
                LogEvent(LogLevels.Fatal, exception.ToString());
            };

            TaskScheduler.UnobservedTaskException += (object sender, UnobservedTaskExceptionEventArgs e) =>
            {
                var exception = GetInnerException(e.Exception);
                LogEvent(LogLevels.Fatal, exception.ToString());
            };
        }

        public static Exception GetInnerException(Exception exception)
        {
            while(true)
            {
                if (exception.InnerException == null)
                    return exception;

                exception = exception.InnerException;
            }
        }

        static void Output(LogLevels logLevel, string output, string originalMessage)
        {
            if (ShowConsoleOutput)
            {
                Console.ResetColor();
                if(logLevel >= LogLevels.Warning)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                if (logLevel > LogLevels.Warning)
                    Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine(output);
                Console.ResetColor();
            }

            if(ShowSystemDiagnostics)
                System.Diagnostics.Debug.WriteLine(output);

            if(PubSub != null)
                TaskRunner.Run(() => PubSub.PublishAsync(new LogEvent(logLevel, output, originalMessage)));
        }

        static void LogEvent(LogLevels logLevel, string message)
        {
            if (logLevel >= LogLevel)
            {
                var output = $"[{logLevel} {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}] {message}";

                Output(logLevel, output, message);
            }
        }

        static void LogEvent(LogLevels logLevel, string message, ILogger logger, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            if (logLevel >= LogLevel)
            {
                var name = string.Empty;
                if (logger != null)
                {
                    if (logLevel < LogLevels.Error)
                    {
                        foreach (var ignore in _ignores)
                        {
                            if (logger.LogName.StartsWith(ignore, StringComparison.Ordinal))
                                return;
                        }
                    }
                    name = $" {logger.LogName}:";
                }
                var sender = $"@{memberName}():{Path.GetFileName(sourceFilePath)}:{sourceLineNumber}";
                var output = $"[{logLevel} {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}]{name} {message} ({sender})";

                Output(logLevel, output, message);
            }
        }

        public static void AddIgnoreList(IEnumerable<string> ignores)
        {
            if(ignores != null)
            {
                foreach(var ignore in ignores)
                {
                    if (!string.IsNullOrWhiteSpace(ignore))
                        _ignores.Add(ignore);
                }
            }
        }

        public static void Write(string message, ILogger logger = null)
        {
            var name = string.Empty;
            if (logger != null)
            {
                foreach (var ignore in _ignores)
                {
                    if (logger.LogName.StartsWith(ignore, StringComparison.Ordinal))
                        return;
                }
                name = $" {logger.LogName}:";
            }

            var output = $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}]{name} {message}";
            Output(LogLevels.Trace, output, message);
        }

        public static void Write(object message, ILogger logger = null)
        {
            Write(message.ToString(), logger);
        }

        public static void Write(LogLevels logLevel, string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(logLevel, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }

        static string OutputClickableException(Exception exception)
        {
#if !DEBUG // Visual Studio Output Window Clickable Source File Hack https://stackoverflow.com/questions/12301055/double-click-to-go-to-source-in-output-window
            var output = new System.Text.StringBuilder();
            var lines = exception.ToString().Replace("\r", "").Split('\n');
            foreach (var line in lines)
            {
                var inStart = line.IndexOf(") in ");
                if (inStart > 0)
                {
                    var lineStart = line.IndexOf(".cs:line");
                    if (lineStart > 0 && lineStart > inStart)
                    {
                        try
                        {
                            var start = inStart + 5;
                            var length = lineStart + 3 - start;
                            var file = line.Substring(start, length);
                            var number = int.Parse(line.Substring(lineStart + 8));
                            var text = line.Substring(0, inStart + 1).Trim();
                            output.AppendLine(string.Format("    {0}({1}): {2}", file, number, text));
                        }
                        catch
                        {
                            output.AppendLine(line);
                        }
                    }
                    else
                    {
                        output.AppendLine(line);
                    }
                }
                else
                {
                    output.AppendLine(line);
                }
            }

            return output.ToString();
#else
            return $"{exception.ToString()}\n";
#endif
        }

        public static void IgnoreException(Exception exception, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Trace, OutputClickableException(GetInnerException(exception)), logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void HandleException(Exception exception, LogLevels logLevel = LogLevels.Error, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(logLevel, OutputClickableException(GetInnerException(exception)), logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void HandleException(Exception exception, ILogger logger, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Error, OutputClickableException(GetInnerException(exception)), logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Trace(string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Trace, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Debug(string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Debug, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Info(string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Info, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Warn(string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Warning, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Error(string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Error, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Fatal(string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Fatal, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }
    }
}
