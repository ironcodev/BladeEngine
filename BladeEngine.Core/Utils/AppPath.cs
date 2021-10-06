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
        static string execDir;
        public static string ExecDir
        {
            get
            {
                if (string.IsNullOrEmpty(execDir))
                {
                    execDir = RemoveTrailingSlash(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                }

                return execDir;
            }
        }
        static string callerDir;
        public static string CallerDir
        {
            get
            {
                if (string.IsNullOrEmpty(callerDir))
                {
                    callerDir = RemoveTrailingSlash(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location));
                }

                return callerDir;
            }
        }
        static string domainDir;
        public static string DomainDir
        {
            get
            {
                if (string.IsNullOrEmpty(domainDir))
                {
                    domainDir = RemoveTrailingSlash(AppDomain.CurrentDomain.BaseDirectory);
                }

                return domainDir;
            }
        }
        static string programDir;
        public static string ProgramDir
        {
            get
            {
                if (string.IsNullOrEmpty(programDir))
                {
                    programDir = RemoveTrailingSlash(Path.GetDirectoryName(typeof(AppPath).Assembly.Location));
                }

                return programDir;
            }
        }
    }
}
