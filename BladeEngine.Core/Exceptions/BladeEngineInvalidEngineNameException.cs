using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineInvalidEngineNameException : BladeEngineException
    {
        public BladeEngineInvalidEngineNameException(string validName, string givenName) :
            base($"Engine name mismatch. Engine of this template can only be {validName}. {givenName} is incorrect")
        { }
    }
}
