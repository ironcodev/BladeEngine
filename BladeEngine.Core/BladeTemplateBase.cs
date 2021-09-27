using BladeEngine.Core.Exceptions;
using System;
using System.Collections.Generic;

namespace BladeEngine.Core
{
    public abstract class BladeTemplateBase
    {
        public BladeTemplateBase(BladeEngineBase engine, string path = ".")
        {
            Engine = engine;
            InnerTemplates = new Dictionary<string, BladeTemplateBase>();
            Path = path;
        }
        public string Path { get; private set; }
        public Dictionary<string, BladeTemplateBase> InnerTemplates { get; set; }
        public BladeEngineBase Engine { get; private set; }
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
        private string moduleName;
        public virtual void SetModuleName(string value)
        {
            moduleName = value;
        }
        public virtual string GetModuleName()
        {
            return moduleName;
        }
        protected abstract string GetEngineName();
        protected virtual void SetEngineName(string engineName)
        { }
        public string EngineName
        {
            get
            {
                return GetEngineName();
            }
            set
            {
                var engineName = GetEngineName();

                if (string.IsNullOrEmpty(engineName))
                {
                    SetEngineName(value);
                }
                else
                {
                    if (string.Compare(value, engineName) != 0)
                    {
                        throw new BladeEngineInvalidEngineNameException(engineName, value);
                    }
                }
            }
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
        public BladeTemplateBase(T engine, string path): base(engine, path)
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
