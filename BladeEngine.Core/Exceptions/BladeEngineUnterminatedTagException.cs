using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineUnterminatedTagException : BladeEngineException
    {
        public BladeEngineUnterminatedTagException(int row, int col, string tag) : base(row, col, $"Unterminated {tag}")
        { }
    }
}
