using BladeEngine.Core;
using BladeEngine.Core.Exceptions;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BladeEngine.VisualBasic
{
    public class BladeEngineConfigVisualBasic : BladeEngineConfigBase
    {
        public override string FileExtension => ".vb";
        public bool UseStrongModel { get; set; }
        public string StrongModelType { get; set; }
        public List<string> References { get; set; }
    }
}
