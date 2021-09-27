using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineException : Exception
    {
        public BladeEngineException(string message) : base(message)
        {
        }
        public BladeEngineException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
