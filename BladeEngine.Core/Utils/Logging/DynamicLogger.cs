using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Utils.Logging
{
    public class DynamicLogger : ILogger
    {
        public ILogger Instance { get; private set; }
        private string type;
        protected virtual ILogger CreateFileLogger()
        {
            return new FileLogger();
        }
        public string Type
        {
            get
            {
                return type;
            }
            set
            {
                switch (value?.ToLower())
                {
                    case "console":
                        type = value;
                        Instance = new ConsoleLogger();
                        break;
                    case "debug":
                        type = value;
                        Instance = new DebugLogger();
                        break;
                    case "array":
                        type = value;
                        Instance = new ArrayLogger();
                        break;
                    case "string":
                        type = value;
                        Instance = new StringLogger();
                        break;
                    case "file":
                        type = value;
                        Instance = CreateFileLogger();
                        break;
                    default:
                        type = "null";
                        Instance = null;
                        break;
                }
            }
        }
        public void Log(string message, LogType logType, bool addLogSeparator = true)
        {
            Instance?.Log(message, logType, addLogSeparator);
        }

        public void Reset()
        {
            Instance?.Reset();
        }
    }
}
