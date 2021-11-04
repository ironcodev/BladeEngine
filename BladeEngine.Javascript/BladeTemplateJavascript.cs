using BladeEngine.Core;
using System.Collections.Generic;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Javascript
{
    public class BladeTemplateJavascript : BladeTemplateBase<BladeEngineJavascript>
    {
        public BladeTemplateJavascript(BladeEngineJavascript engine, BladeTemplateSettings settings = null) : base(engine, settings)
        {
            Dependencies = $"import './BladeTemplateJavascriptBase';";
            ClassPath = new List<string>();
        }
        public List<string> ClassPath { get; }
        public override string RenderContent()
        {
            return $@"
{ExternalCode}
    class {GetMainClassName()} extends BladeTemplateJavascriptBase {{
        render() {{
            {Body}
            const result = _buffer.join('');

            _buffer = new [];

            return result;
        }}
        {Functions}
    }}
";
        }
        protected override string GetEngineName()
        {
            return "Javascript";
        }
    }
}
