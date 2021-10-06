using BladeEngine.Core;
using BladeEngine.Core.Exceptions;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BladeEngine.CSharp
{
    public class BladeEngineConfigCSharp : BladeEngineConfigBase
    {
        public override string FileExtension => ".cs";
        public bool UseStrongModel { get; set; }
        public string StrongModelType { get; set; }
        public List<string> References { get; set; }
    }
}
