using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineIncludePathUnderflowException : BladeEngineException
    {
        public BladeEngineIncludePathUnderflowException(int row, int col, string includePath) : base(row, col, $"Include path {includePath} surpasses root in going back")
        { }
    }
}
