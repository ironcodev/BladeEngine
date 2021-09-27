using System;
using BladeEngine.Core;
using BladeEngine.Core.Exceptions;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.CSharp
{
    public class BladeEngineCSharp : BladeEngineBase<BladeEngineConfigCSharp>
    {
        public BladeEngineCSharp(): this(new BladeEngineConfigCSharp())
        { }
        public BladeEngineCSharp(BladeEngineConfigCSharp config) : base(config)
        { }
        protected override string WriteLiteral(string str)
        {
            return string.IsNullOrEmpty(str) ? "" : $"{Environment.NewLine}_buffer.Append(@\"{str.Replace("\"", "\"\"")}\");";
        }
        protected override BladeTemplateBase CreateTemplate(string path)
        {
            return new BladeTemplateCSharp(this, path);
        }
        protected override string WriteValue(string str)
        {
            return $"{Environment.NewLine}_buffer.Append({str});";
        }
        protected override string MergeDependencies(string currentDependencies, string newDependencies)
        {
            return MergeDependencies(currentDependencies, newDependencies, "using", ";", (list, current) =>
            {
                var result = current;
                var eqIndex = current.IndexOf('=');

                if (eqIndex >= 0)
                {
                    var left = current.Substring(0, eqIndex).Trim();
                    var right = current.Substring(eqIndex + 1).Trim();

                    result = left + "=" + right;
                }

                return result;
            });
        }

        protected override void OnIncludeTemplate(BladeTemplateBase current, BladeTemplateBase include)
        {
            current.Dependencies = Try(() => MergeDependencies(current.Dependencies, include.Dependencies), e => new BladeEngineMergeDependenciesException(include.Path, e));
            current.ExternalCode += Environment.NewLine + include.ExternalCode + Environment.NewLine + include.Body;
        }
    }
}
