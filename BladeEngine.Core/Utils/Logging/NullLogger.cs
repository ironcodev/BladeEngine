using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Utils.Logging
{
    public class NullLogger : ILogger
    {
        public void Log(string message, LogType logType, bool addLogSeparator = true)
        {
        }

        public void Reset()
        {
        }
    }
}
