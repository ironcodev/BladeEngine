﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Utils.Logging
{
    public enum LogType
    {
        Default,
        Info,
        Success,
        Danger,
        Warning,
        Debug
    }
    public interface ILogger
    {
        void Log(string message, LogType logType, bool addLogSeparator = true);
        void Reset();
    }
}
