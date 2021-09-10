using System;

namespace BladeEngine.Core
{
    public abstract class BladeTemplateBase
    {
        public BladeEngineBase Engine { get; private set; }
        public BladeTemplateBase(BladeEngineBase engine)
        {
            Engine = engine;
        }
        private string mainClassName;
        public virtual void SetMainClassName(string value)
        {
            mainClassName = value;
        }
        public virtual string GetMainClassName()
        {
            if (string.IsNullOrEmpty(mainClassName))
            {
                mainClassName = $"BladeTemplate{Guid.NewGuid().ToString().Replace("-", "")}";
            }

            return mainClassName;
        }
        public string Dependencies { get; set; }
        public string Functions { get; set; }
        public string ExternalCode { get; set; }
        public string Body { get; set; }
        public virtual string RenderDependencies()
        {
            return Dependencies + Environment.NewLine;
        }
        public abstract string RenderContent();
        public virtual string Render()
        {
            return $@"
{RenderDependencies()}
{RenderContent()}
";
        }
    }
    public abstract class BladeTemplateBase<T>: BladeTemplateBase
        where T: BladeEngineBase
    {
        public BladeTemplateBase(T engine): base(engine)
        { }
        public T StrongEngine
        {
            get
            {
                return (T)Engine;
            }
        }
    }
}
