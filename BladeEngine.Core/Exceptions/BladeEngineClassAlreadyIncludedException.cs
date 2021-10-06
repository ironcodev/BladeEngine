using System;
using BladeEngine.Core.Exceptions;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineClassAlreadyIncludedException : BladeEngineException
    {
        public BladeEngineClassAlreadyIncludedException(int row, int col, string className) : base(row, col, $"class '{className}' already included.")
        { }
    }
}
