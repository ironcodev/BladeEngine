using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Base.Exceptions
{
    [Serializable]
    public class BladeEngineUnterminatedTagException : BladeEngineException
    {
        public BladeEngineUnterminatedTagException(string tag) : base($"Unterminated {tag}")
        { }
    }
}
