using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineIncludeFileReadException : BladeEngineException
    {
        public BladeEngineIncludeFileReadException(int row, int col, string includePath, Exception e) : base(row, col, $"Error while reading include file '{includePath}'", e)
        { }
    }
}
