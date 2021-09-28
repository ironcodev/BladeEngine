using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineUnterminatedFunctionException : BladeEngineException
    {
        public BladeEngineUnterminatedFunctionException(int row, int col) : base(row, col, "function is not closed.")
        { }
    }
}
