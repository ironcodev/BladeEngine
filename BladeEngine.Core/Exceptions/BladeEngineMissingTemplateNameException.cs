using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core.Base.Exceptions
{
    public class BladeEngineMissingTemplateNameException : BladeEngineException
    {
        public BladeEngineMissingTemplateNameException() : base("Missing template name. No template name is specified.")
        {
        }
    }
}
