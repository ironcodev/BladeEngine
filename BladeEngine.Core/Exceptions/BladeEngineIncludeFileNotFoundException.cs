using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Base.Exceptions
{
    [Serializable]
    public class BladeIncludeFileNotFoundException : BladeEngineException
    {
        public BladeIncludeFileNotFoundException(string includePath) : base($"Include file {includePath} not found")
        { }
    }
}
