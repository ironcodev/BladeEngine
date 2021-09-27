using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineUnterminatedTagException : BladeEngineException
    {
        public BladeEngineUnterminatedTagException(string tag) : base($"Unterminated {tag}")
        { }
    }
}
