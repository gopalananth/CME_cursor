using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Chef_Middle_East_Form.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly string _logFilePath;
        private readonly bool _enableFileLogging;
        private readonly bool _enableTraceLogging;

        public LoggingService()
        {
            _logFilePath = ConfigurationManager.AppSettings["LogFilePath"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "application.log");
            _enableFileLogging = bool.Parse(ConfigurationManager.AppSettings["EnableFileLogging"] ?? "true");
            _enableTraceLogging = bool.Parse(ConfigurationManager.AppSettings["EnableTraceLogging"] ?? "true");

            // Ensure log directory exists
            if (_enableFileLogging)
            {
                var logDir = Path.GetDirectoryName(_logFilePath);
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
            }
        }

        public void LogInfo(string message, object additionalData = null)
        {
            WriteLog("INFO", message, null, additionalData);
        }

        public void LogWarning(string message, object additionalData = null)
        {
            WriteLog("WARN", message, null, additionalData);
        }

        public void LogError(string message, Exception exception = null, object additionalData = null)
        {
            WriteLog("ERROR", message, exception, additionalData);
        }

        public string LogException(Exception exception, object additionalContext = null)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            var message = $"Exception occurred [ErrorID: {errorId}]";
            
            WriteLog("ERROR", message, exception, additionalContext);
            
            return errorId;
        }

        private void WriteLog(string level, string message, Exception exception = null, object additionalData = null)
        {
            var timestamp = DateTime.UtcNow;
            var logEntry = new
            {
                Timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff UTC"),
                Level = level,
                Message = message,
                Exception = exception != null ? new
                {
                    Type = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                } : null,
                AdditionalData = additionalData,
                MachineName = Environment.MachineName,
                ProcessId = Process.GetCurrentProcess().Id
            };

            var logText = JsonConvert.SerializeObject(logEntry, Formatting.None);

            // Write to trace if enabled
            if (_enableTraceLogging)
            {
                Trace.WriteLine($"[{timestamp:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
                if (exception != null)
                {
                    Trace.WriteLine($"Exception: {exception.Message}");
                    Trace.WriteLine($"StackTrace: {exception.StackTrace}");
                }
            }

            // Write to file if enabled
            if (_enableFileLogging)
            {
                try
                {
                    using (var writer = new StreamWriter(_logFilePath, true, Encoding.UTF8))
                    {
                        writer.WriteLine(logText);
                    }
                }
                catch (Exception ex)
                {
                    // Fallback to trace if file logging fails
                    Trace.WriteLine($"Failed to write to log file: {ex.Message}");
                    Trace.WriteLine($"Original log entry: {logText}");
                }
            }
        }
    }
}