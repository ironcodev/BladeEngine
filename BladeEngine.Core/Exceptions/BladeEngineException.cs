using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Base.Exceptions
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
