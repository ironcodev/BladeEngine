using System;
using BladeEngine.Core;

namespace BladeEngine.CSharp
{
    public class BladeEngineCSharp : BladeEngineBase<BladeEngineConfigCSharp>
    {
        public BladeEngineCSharp() : this(new BladeEngineConfigCSharp())
        { }
        public BladeEngineCSharp(BladeEngineConfigCSharp config) : base(config)
        { }
        protected override string WriteLiteral(string str)
        {
            return string.IsNullOrEmpty(str) ? "" : $"{Environment.NewLine}_buffer.Append(@\"{str.Replace("\"", "\"\"")}\");";
        }
        protected override BladeTemplateBase CreateTemplate()
        {
            return new BladeTemplateCSharp(this);
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
    }
}
