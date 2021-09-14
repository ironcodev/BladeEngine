using System;
using System.IO;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core.Utils.Logging
{
    public class FileLogger : ILogger
    {
        public string FileName { get; set; }
        public string Separator { get; set; }
        public FileLogger(string filename = "log.txt", string separator = null)
        {
            if (string.IsNullOrEmpty(filename))
            {
                filename = "log.txt";
            }

            if (Path.IsPathRooted(filename))
            {
                FileName = filename;
            }
            else
            {
                FileName = Path.Combine(AppPath.DomainDir, filename);
            }

            if (string.IsNullOrEmpty(separator))
            {
                separator = new string('-', 100);
            }

            Separator = separator;
        }
        public void Log(string message, LogType logType, bool addLogSeparator = true)
        {
            Try(() => { File.AppendAllText(FileName, $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff")}: {message}{Environment.NewLine}"); return true; });

            if (addLogSeparator)
            {
                Try(() => { File.AppendAllText(FileName, Separator + Environment.NewLine); return true; });
            }
        }
        public void Reset()
        {
            Try(() => { File.WriteAllText(FileName, ""); return true; });
        }
    }
}
