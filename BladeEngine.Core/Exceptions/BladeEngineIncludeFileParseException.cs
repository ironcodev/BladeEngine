using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineIncludeFileParseException : BladeEngineException
    {
        public BladeEngineIncludeFileParseException(string includePath, Exception e) : base($"Error while parsing include file '{includePath}'.", e)
        { }
    }
}
