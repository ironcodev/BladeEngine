using System;

namespace BladeEngine.Core.Exceptions
{
    public class BladeEngineMissingTemplateNameException : BladeEngineException
    {
        public BladeEngineMissingTemplateNameException(int row, int col) : base(row, col, "Missing template name. No template name is specified.")
        {
        }
    }
}
