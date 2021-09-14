using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Utils.Logging
{
    public class ArrayLogger : ILogger, IEnumerable<ArrayLogger.LogItem>
    {
        public class LogItem
        {
            public string Message { get; set; }
            public DateTime LogDate { get; set; }
            public LogType Type { get; set; }
        }
        private List<LogItem> logs;
        public ArrayLogger()
        {
            logs = new List<LogItem>();
        }
        public void Log(string message, LogType logType, bool addLogSeparator = true)
        {
            logs.Add(new LogItem { Message = message, LogDate = DateTime.Now, Type = logType });
        }
        public void Reset()
        {
            logs.Clear();
        }
        public LogItem[] GetAll()
        {
            return logs.ToArray();
        }
        public IEnumerator<LogItem> GetEnumerator()
        {
            foreach (var log in logs)
            {
                yield return log;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
