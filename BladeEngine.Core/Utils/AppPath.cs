using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core.Utils
{
    public static class AppPath
    {
        private static string RemoveTrailingSlash(string path)
        {
            if (IsSomeString(path))
            {
                var ch = path[path.Length - 1];

                if (ch == '\\' || ch == '/')
                {
                    return path.Substring(0, path.Length - 1);
                }
            }

            return path;
        }
        public static string ExecDir
        {
            get
            {
                return RemoveTrailingSlash(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            }
        }
        public static string CallerDir
        {
            get
            {
                return RemoveTrailingSlash(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location));
            }
        }
        public static string DomainDir
        {
            get
            {
                return RemoveTrailingSlash(AppDomain.CurrentDomain.BaseDirectory);
            }
        }
    }
}
