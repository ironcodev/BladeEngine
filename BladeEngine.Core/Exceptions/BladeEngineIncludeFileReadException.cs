using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Base.Exceptions
{
    [Serializable]
    public class BladeEngineIncludeFileReadException : BladeEngineException
    {
        public BladeEngineIncludeFileReadException(string includePath, Exception e) : base($"Error while reading include file '{includePath}'", e)
        { }
    }
}
