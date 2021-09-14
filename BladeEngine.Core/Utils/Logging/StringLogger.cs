using System;
using System.Text;

namespace BladeEngine.Core.Utils.Logging
{
    public class StringLogger : ILogger
    {
        private StringBuilder builder;
        public string Separator { get; set; }
        public StringLogger(string separator = null)
        {
            builder = new StringBuilder();

            if (string.IsNullOrEmpty(separator))
            {
                separator = new string('-', 100);
            }

            Separator = separator;
        }
        public void Log(string message, LogType logType, bool addLogSeparator = true)
        {
            builder.Append($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff")}: {message}");

            if (addLogSeparator)
            {
                builder.Append(Separator);
            }
        }
        public override string ToString()
        {
            return builder.ToString();
        }
        public void Reset()
        {
            builder.Clear();
        }
    }
}
