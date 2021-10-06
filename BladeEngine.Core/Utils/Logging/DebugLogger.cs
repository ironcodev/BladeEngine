using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BladeEngine.Core.Utils.Logging
{
    public class DebugLogger: ILogger
    {
        public void Log(string message, LogType logType, bool addLogSeparator = true)
        {
            switch (logType)
            {
                case LogType.Info:
                    Debug.Write(message);
                    break;
                case LogType.Success:
                    Debug.Write(message);
                    break;
                case LogType.Danger:
                    Debug.Fail(message);
                    break;
                case LogType.Warning:
                    Debug.Write(message);
                    break;
                case LogType.Debug:
                    Debug.Assert(false, message);
                    break;
                default:
                    Debug.Write(message);
                    break;
            }

            if (addLogSeparator)
            {
                Debug.Write(Environment.NewLine);
            }
        }
        public void Reset()
        {
            Debug.Flush();
        }
    }
}
