using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineInvalidClassNameException : BladeEngineException
    {
        public BladeEngineInvalidClassNameException(string name) : base($"class name '{name}' is invalid.")
        { }
    }
}
