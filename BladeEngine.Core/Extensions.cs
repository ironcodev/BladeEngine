using BladeEngine.Core.Utils;
using BladeEngine.Core.Utils.Logging;
using System;
using System.Text;
using System.Text.RegularExpressions;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core
{
    public static class Extensions
    {
        public static bool DescendsFrom(this Type type, Type targetType)
        {
            var result = false;

            if (type != null)
            {
                if (targetType == null) throw new ArgumentNullException("targetType");

                result = type.IsSubclassOf(targetType);

                if (!result)
                {
                    if (targetType.IsGenericTypeDefinition)
                    {
                        var _type = type;

                        while (_type != typeof(object) && _type != null)
                        {
                            if (_type.IsGenericType && _type.GetGenericTypeDefinition() == targetType)
                            {
                                result = true;
                                break;
                            }

                            _type = _type.BaseType;
                        }
                    }
                }
            }

            return result;
        }
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
        public static bool IsSucceeded(this ShellExecuteResponse sr, string expectedOutput = null, bool ignoreCase = true, bool regexExpectedOutput = false)
        {
            var result = sr.Succeeded && string.IsNullOrEmpty(sr.Errors);

            if (result && expectedOutput != null)
            {
                if (regexExpectedOutput)
                {
                    result = Regex.IsMatch(sr.Output ?? "", expectedOutput);
                }
                else
                {
                    result = string.Compare(sr.Output ?? "", expectedOutput, ignoreCase) == 0;
                }
            }

            return result;
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
        public static void Info(this ILogger logger, string message)
        {
            logger.Log(message, LogType.Info);
        }
        public static void Success(this ILogger logger, string message)
        {
            logger.Log(message, LogType.Success);
        }
        public static void Danger(this ILogger logger, string message)
        {
            logger.Log(message, LogType.Danger);
        }
        public static void Warn(this ILogger logger, string message)
        {
            logger.Log(message, LogType.Warning);
        }
        public static void Debug(this ILogger logger, string message)
        {
            logger.Log(message, LogType.Debug);
        }
        #endregion
        public static bool Try(this ILogger logger, string message, bool debug, Action action)
        {
            var result = false;

            if (debug)
            {
                logger.Log(Environment.NewLine + message);
            }

            try
            {
                action();

                result = true;

                if (debug)
                {
                    logger.Success("Succeeded");
                }
            }
            catch (Exception e)
            {
                if (debug)
                {
                    logger.Danger("Failed");
                    logger.Danger(e.ToString(Environment.NewLine));
                }
            }

            return result;
        }
        public static T Try<T>(this ILogger logger, string message, bool debug, Func<T> action, T defaultValue = default)
        {
            var result = defaultValue;

            if (debug)
            {
                logger.Log(Environment.NewLine + message);
            }

            try
            {
                result = action();

                if (debug)
                {
                    logger.Success("Succeeded");
                }
            }
            catch (Exception e)
            {
                if (debug)
                {
                    logger.Danger("Failed");
                    logger.Danger(e.ToString(Environment.NewLine));
                }
            }

            return result;
        }
        public static bool Try(this ILogger logger, string message, bool debug, Func<bool> action, bool defaultValue = default)
        {
            var result = defaultValue;

            if (debug)
            {
                logger.Log(Environment.NewLine + message);
            }

            try
            {
                result = action();

                if (debug)
                {
                    if (result)
                    {
                        logger.Success("Succeeded");
                    }
                    else
                    {
                        logger.Warn("Failed");
                    }
                }
            }
            catch (Exception e)
            {
                if (debug)
                {
                    logger.Danger("Failed");
                    logger.Danger(e.ToString(Environment.NewLine));
                }
            }

            return result;
        }
        public static bool Try(this ILogger logger, string message, Func<bool> action, bool defaultValue = default)
        {
            var result = defaultValue;

            logger.Log(Environment.NewLine + message);

            try
            {
                result = action();

                if (result)
                {
                    logger.Success("Succeeded");
                }
                else
                {
                    logger.Warn("Failed");
                }
            }
            catch (Exception e)
            {
                logger.Danger("Failed");
                logger.Danger(e.ToString(Environment.NewLine));
            }

            return result;
        }
        public static void Abort(this ILogger logger, string message, bool show)
        {
            if (show)
            {
                logger.Log($"{message}. Use -debug for more details.");
            }
        }
    }
}
