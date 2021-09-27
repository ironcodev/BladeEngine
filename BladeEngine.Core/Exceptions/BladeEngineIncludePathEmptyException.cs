using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineIncludePathEmptyException : BladeEngineException
    {
        public BladeEngineIncludePathEmptyException() : base($"No include path specified")
        { }
    }
}
