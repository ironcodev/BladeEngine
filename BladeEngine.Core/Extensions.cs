using System;
using System.Text;

namespace BladeEngine.Core
{
    public static class Extensions
    {
        public static string ToString(this Exception e, string separator)
        {
            var result = new StringBuilder();
            var current = e;

            while (current != null)
            {
                result.Append(e.Message + separator);

                current = current.InnerException;
            }

            return result.ToString();
        }
        #region Logger Extensions
        public static void Log(this ILogger logger, Exception e, LogType logType = LogType.Danger)
        {
            logger.Log(e.ToString(Environment.NewLine), logType);
        }
        public static void Log(this ILogger logger, string message)
        {
            logger.Log(message, LogType.Default);
        }
        public static void LogLn(this ILogger logger, string message)
        {
            logger.Log(message + Environment.NewLine, LogType.Default);
        }
        public static void Info(this ILogger logger, string message)
        {
            logger.Log(message, LogType.Info);
        }
        public static void InfoLn(this ILogger logger, string message)
        {
            logger.Log(message + Environment.NewLine, LogType.Info);
        }
        public static void Success(this ILogger logger, string message)
        {
            logger.Log(message, LogType.Success);
        }
        public static void SuccessLn(this ILogger logger, string message)
        {
            logger.Log(message + Environment.NewLine, LogType.Success);
        }
        public static void Danger(this ILogger logger, string message)
        {
            logger.Log(message, LogType.Danger);
        }
        public static void DangerLn(this ILogger logger, string message)
        {
            logger.Log(message + Environment.NewLine, LogType.Danger);
        }
        public static void Warn(this ILogger logger, string message)
        {
            logger.Log(message, LogType.Warning);
        }
        public static void WarnLn(this ILogger logger, string message)
        {
            logger.Log(message + Environment.NewLine, LogType.Warning);
        }
        public static void Debug(this ILogger logger, string message)
        {
            logger.Log(message, LogType.Debug);
        }
        public static void DebugLn(this ILogger logger, string message)
        {
            logger.Log(message + Environment.NewLine, LogType.Debug);
        }
        #endregion
    }
}
