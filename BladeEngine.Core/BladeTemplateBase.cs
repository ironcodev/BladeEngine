using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BladeEngine.Core.Exceptions;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core
{
    public abstract class BladeTemplateBase
    {
        protected string ClassNameRegex;
        protected string ModuleNameRegex;
        public BladeTemplateBase(BladeEngineBase engine, BladeTemplateSettings settings = null)
        {
            if (settings == null)
            {
                settings = new BladeTemplateSettings();
            }

            Settings = settings;
            Engine = engine;
            InnerTemplates = new List<BladeTemplateBase>();
            ClassNameRegex = @"^[A-Za-z_]\w*$";
            ModuleNameRegex = @"^[a-z_A-Z]\w*(\.[a-z_A-Z]\w*)*$";
        }

        public BladeTemplateSettings Settings { get; }
        public List<BladeTemplateBase> InnerTemplates { get; }
        public BladeEngineBase Engine { get; private set; }
        protected string mainClassName;
        public bool HasUserClassName { get; protected set; }
        public virtual void SetMainClassName(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (Regex.Match(value, ClassNameRegex).Success)
                {
                    mainClassName = value;
                    HasUserClassName = IsSomeString(value, rejectAllWhitespaceStrings: true);
                }
                else
                {
                    throw new BladeEngineInvalidClassNameException(value);
                }
            }
            else
            {
                mainClassName = "";
            }
        }
        public virtual string GetMainClassName(bool autoGenerateModuleName = true)
        {
            if (!IsSomeString(mainClassName, rejectAllWhitespaceStrings: true) && autoGenerateModuleName)
            {
                mainClassName = $"BladeTemplate{Guid.NewGuid().ToString().Replace("-", "")}";
            }

            return mainClassName;
        }
        public virtual string GetFullMainClassName()
        {
            return GetModuleName() + "." + GetMainClassName();
        }
        protected string moduleName;
        public virtual void SetModuleName(string value)
        {
            if (IsSomeString(value, rejectAllWhitespaceStrings: true))
            {
                if (Regex.Match(value, ModuleNameRegex).Success)
                {
                    moduleName = value;
                }
                else
                {
                    throw new BladeEngineInvalidModuleNameException(value);
                }
            }
            else
            {
                moduleName = "";
            }
        }
        public virtual string GetModuleName(bool autoGenerateModuleName = true)
        {
            if (!IsSomeString(moduleName, rejectAllWhitespaceStrings: true) && autoGenerateModuleName)
            {
                moduleName = $"Blade{Guid.NewGuid().ToString().Replace("-", "")}";
            }

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
        public BladeTemplateBase(T engine, BladeTemplateSettings settings = null) : base(engine, settings)
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
