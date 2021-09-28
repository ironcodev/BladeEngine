using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineIncludeFileParseException : BladeEngineException
    {
        public BladeEngineIncludeFileParseException(int row, int col, string includePath, Exception e) : base(row, col, $"Error while parsing include file '{includePath}'.", e)
        { }
    }
}
