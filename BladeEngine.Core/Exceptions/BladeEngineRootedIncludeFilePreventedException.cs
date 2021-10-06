using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineRootedIncludeFilePreventedException : BladeEngineException
    {
        public BladeEngineRootedIncludeFilePreventedException(int row, int col, string includePath) : base(row, col, $"Using absolute path in include files is not allowed ('{includePath}').")
        { }
    }
}
