using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineIncludeFileReadException : BladeEngineException
    {
        public BladeEngineIncludeFileReadException(string includePath, Exception e) : base($"Error while reading include file '{includePath}'", e)
        { }
    }
}
