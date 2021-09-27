using System;

namespace BladeEngine.Core.Exceptions
{
    public class BladeEngineMergeDependenciesException: BladeEngineException
    {
        public BladeEngineMergeDependenciesException(string includePath, Exception e): base($"Error while merging include file '{includePath}' dependencies", e)
        { }
    }
}
