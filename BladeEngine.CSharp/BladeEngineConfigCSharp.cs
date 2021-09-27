using BladeEngine.Core;
using System.Collections.Generic;

namespace BladeEngine.CSharp
{
    public class BladeEngineConfigCSharp : BladeEngineConfigBase
    {
        public override string FileExtension => ".cs";
        private string @namespace;
        public string Namespace
        {
            get
            {
                if (string.IsNullOrEmpty(@namespace))
                {
                    @namespace = "Blade";
                }

                return @namespace;
            }
            set
            {
                @namespace = value;
            }
        }
        public bool UseStrongModel { get; set; }
        public string StrongModelType { get; set; }
        public List<string> References { get; set; }
    }
}
