using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeIncludeFileNotFoundException : BladeEngineException
    {
        public BladeIncludeFileNotFoundException(int row, int col, string includePath) : base(row, col, $"Include file {includePath} not found")
        { }
    }
}
