using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Base.Exceptions
{
    [Serializable]
    public class BladeEngineIncludeFileParseException : BladeEngineException
    {
        public BladeEngineIncludeFileParseException(string includePath, Exception e) : base($"Error while parsing include file '{includePath}'.", e)
        { }
    }
}
