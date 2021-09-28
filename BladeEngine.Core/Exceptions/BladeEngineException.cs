using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineException : Exception
    {
        public BladeEngineException(string message) : base(message)
        {
        }
        public BladeEngineException(int row, int col, string message) : base($"Line: {row}, {col}: {message}")
        {
        }
        public BladeEngineException(string message, Exception innerException) : base(message, innerException)
        {
        }
        public BladeEngineException(int row, int col, string message, Exception innerException) : base($"Line: {row}, {col}: {message}", innerException)
        {
        }
    }
}
