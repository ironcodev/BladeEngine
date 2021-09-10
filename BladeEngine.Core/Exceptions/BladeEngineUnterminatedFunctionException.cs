using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Base.Exceptions
{
    [Serializable]
    public class BladeEngineUnterminatedFunctionException : BladeEngineException
    {
        public BladeEngineUnterminatedFunctionException() : base("function is not closed.")
        { }
    }
}
