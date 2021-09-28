using System;

namespace BladeEngine.Core.Exceptions
{
    public class BladeEngineMergeDependenciesException: BladeEngineException
    {
        public BladeEngineMergeDependenciesException(int row, int col, string includePath, Exception e): base(row, col, $"Error while merging include file '{includePath}' dependencies", e)
        { }
    }
}
