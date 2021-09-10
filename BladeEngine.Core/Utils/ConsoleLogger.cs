using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core
{
    public class ConsoleLogger: ILogger
    {
        public void Log(string message, LogType logType)
        {
            var color = Console.ForegroundColor;

            switch (logType)
            {
                case LogType.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(message);
                    break;
                case LogType.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(message);
                    break;
                case LogType.Danger:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(message);
                    break;
                case LogType.Warning:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write(message);
                    break;
                case LogType.Debug:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(message);
                    break;
                default:
                    Console.Write(message);
                    break;
            }

            Console.ForegroundColor = color;
        }
    }
}
