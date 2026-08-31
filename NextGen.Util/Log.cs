using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace NextGen.Util
{
    public static class Log
    {
        private static ILogger _logger;
        private static bool _isDebug;

        public static bool IsDebug
        {
            get => _isDebug;
            set => _isDebug = value;
        }

        public static void Initialize(string serverName)
        {
            string logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs", serverName);
            Directory.CreateDirectory(logDir);

            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    Path.Combine(logDir, $"{serverName}-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        public static void WriteLine(LogLevel level, string format, params object[] args)
        {
            if (_logger == null)
                throw new InvalidOperationException("Log.Initialize() must be called first.");

            if (level == LogLevel.Debug && !_isDebug)
                return;

            var message = string.Format(format, args);

            switch (level)
            {
                case LogLevel.Debug:
                    _logger.Debug(message);
                    break;
                case LogLevel.Info:
                    _logger.Information(message);
                    break;
                case LogLevel.Warn:
                    _logger.Warning(message);
                    break;
                case LogLevel.Error:
                    _logger.Error(message);
                    break;
                case LogLevel.Exception:
                    _logger.Fatal(message);
                    break;
                default:
                    _logger.Information(message);
                    break;
            }
        }

        public static void Debug(string message) => WriteLine(LogLevel.Debug, message);
        public static void Info(string message) => WriteLine(LogLevel.Info, message);
        public static void Warn(string message) => WriteLine(LogLevel.Warn, message);
        public static void Error(string message) => WriteLine(LogLevel.Error, message);
        public static void Fatal(string message) => WriteLine(LogLevel.Exception, message);

        public static void SetLogToFile(string filename)
        {
            // Kept for backward compatibility; logging to file is now handled by Serilog
        }
    }
}
