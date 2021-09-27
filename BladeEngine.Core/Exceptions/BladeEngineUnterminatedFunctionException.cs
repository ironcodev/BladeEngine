using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineUnterminatedFunctionException : BladeEngineException
    {
        public BladeEngineUnterminatedFunctionException() : base("function is not closed.")
        { }
    }
}
