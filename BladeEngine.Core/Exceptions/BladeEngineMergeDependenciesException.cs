using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Base.Exceptions
{
    public class BladeEngineMergeDependenciesException: BladeEngineException
    {
        public BladeEngineMergeDependenciesException(string includePath, Exception e): base($"Error while merging include file '{includePath}' dependencies", e)
        { }
    }
}
