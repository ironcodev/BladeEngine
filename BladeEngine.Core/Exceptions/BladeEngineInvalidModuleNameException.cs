using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineInvalidModuleNameException : BladeEngineException
    {
        public BladeEngineInvalidModuleNameException(string name) : base($"class name '{name}' is invalid.")
        { }
    }
}
