using System;
using System.Collections.Generic;
using System.Text;

namespace BladeEngine.Core
{
    public class BladeEngineConfigAny : BladeEngineConfigBase
    {
        public override string FileExtension => "*";
    }
    public class BladeTemplateAny : BladeTemplateBase
    {
        public BladeTemplateAny(BladeEngineBase engine, string path = ".") : base(engine, path)
        { }
        private string engineName;
        public override string RenderContent()
        {
            throw new NotImplementedException();
        }

        protected override string GetEngineName()
        {
            return engineName;
        }

        protected override void SetEngineName(string engineName)
        {
            this.engineName = engineName;
        }
    }
    public class BladeEngineAny : BladeEngineBase
    {
        public BladeEngineAny(BladeEngineConfigAny config): base(config)
        { }
        protected override BladeTemplateBase CreateTemplate(string path)
        {
            return new BladeTemplateAny(this);
        }
        public override BladeTemplateBase Parse(string template, string path = ".")
        {
            endParseOnFirstEngineNameOccuranceDetection = true;

            var result = base.Parse(template);

            endParseOnFirstEngineNameOccuranceDetection = false;

            return result;
        }
        protected override string MergeDependencies(string currentDependencies, string newDependencies)
        {
            return "";
        }
        protected override string WriteLiteral(string literal)
        {
            return "";
        }
        protected override string WriteValue(string value)
        {
            return "";
        }

        protected override void OnIncludeTemplate(BladeTemplateBase current, BladeTemplateBase include)
        {
        }
    }
}
