using System;

namespace BladeEngine.Core.Exceptions
{
    public class BladeEngineMissingTemplateNameException : BladeEngineException
    {
        public BladeEngineMissingTemplateNameException() : base("Missing template name. No template name is specified.")
        {
        }
    }
}
