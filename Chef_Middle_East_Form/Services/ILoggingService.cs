using System;

namespace Chef_Middle_East_Form.Services
{
    public interface ILoggingService
    {
        /// <summary>
        /// Logs an information message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="additionalData">Optional additional data to include</param>
        void LogInfo(string message, object additionalData = null);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="additionalData">Optional additional data to include</param>
        void LogWarning(string message, object additionalData = null);

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="exception">Optional exception details</param>
        /// <param name="additionalData">Optional additional data to include</param>
        void LogError(string message, Exception exception = null, object additionalData = null);

        /// <summary>
        /// Logs an exception with a unique error ID for tracking
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="additionalContext">Additional context information</param>
        /// <returns>Unique error ID for reference</returns>
        string LogException(Exception exception, object additionalContext = null);
    }
}