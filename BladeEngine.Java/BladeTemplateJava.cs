using BladeEngine.Core;
using System.Collections.Generic;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Java
{
    public class BladeTemplateJava : BladeTemplateBase<BladeEngineJava>
    {
        public BladeTemplateJava(BladeEngineJava engine, BladeTemplateSettings settings = null) : base(engine, settings)
        {
            Dependencies = $"import Blade.BladeTemplateJavaBase;";
            ClassPath = new List<string>();
        }
        public List<string> ClassPath { get; }
        public override string RenderContent()
        {
            return $@"
{ExternalCode}
    public class {GetMainClassName()} extends BladeTemplateJavaBase {{
        public String render() {{
            {Body}
            String result = _buffer.toString();

            _buffer.setLength(0);

            return result;
        }}
        {Functions}
    }}
";
        }
        protected override string GetEngineName()
        {
            return "java";
        }
    }
}
