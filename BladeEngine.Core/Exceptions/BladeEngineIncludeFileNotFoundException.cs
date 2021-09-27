using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeIncludeFileNotFoundException : BladeEngineException
    {
        public BladeIncludeFileNotFoundException(string includePath) : base($"Include file {includePath} not found")
        { }
    }
}
