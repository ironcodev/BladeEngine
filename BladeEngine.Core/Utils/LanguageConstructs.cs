using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Utils
{
    public static class LanguageConstructs
    {
        public static T Try<T>(Func<T> action, T errorValue = default)
        {
            T result;

            try
            {
                result = action();
            }
            catch
            {
                result = errorValue;
            }

            return result;
        }
        public static T Try<T>(Func<T> action, Func<Exception, Exception> thrownException)
        {
            T result;

            try
            {
                result = action();
            }
            catch (Exception e)
            {
                if (thrownException != null)
                {
                    throw thrownException(e);
                }
                else
                {
                    throw;
                }
            }

            return result;
        }
        public static bool Try<T>(Func<T> action, out T result, out Exception ex)
        {
            var success = false;

            result = default;
            ex = null;

            try
            {
                result = action();
            }
            catch (Exception e)
            {
                ex = e;
            }

            return success;
        }
    }
}
