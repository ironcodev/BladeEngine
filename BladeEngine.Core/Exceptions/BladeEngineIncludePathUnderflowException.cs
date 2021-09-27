using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineIncludePathUnderflowException : BladeEngineException
    {
        public BladeEngineIncludePathUnderflowException(string includePath) : base($"Include path {includePath} surpasses root in going back")
        { }
    }
}
