using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineIncludePathEmptyException : BladeEngineException
    {
        public BladeEngineIncludePathEmptyException(int row, int col) : base(row, col, $"No include path specified")
        { }
    }
}
