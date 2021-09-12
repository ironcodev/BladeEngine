using System;
using BladeEngine.Core;

namespace BladeEngine.Java
{
    public class BladeEngineJava : BladeEngineBase<BladeEngineConfigJava>
    {
        public BladeEngineJava(): this(new BladeEngineConfigJava())
        { }
        public BladeEngineJava(BladeEngineConfigJava config) : base(config)
        {
            Config.CamelCase = true;
        }
        protected override string WriteLiteral(string str)
        {
            return string.IsNullOrEmpty(str) ? "" : $"{Environment.NewLine}_buffer.Append(@\"{str.Replace("\"", "\"\"")}\");";
        }
        protected override BladeTemplateBase CreateTemplate()
        {
            return new BladeTemplateJava(this);
        }
        protected override string WriteValue(string str)
        {
            return $"{Environment.NewLine}_buffer.Append({str});";
        }
        protected override string MergeDependencies(string currentDependencies, string newDependencies)
        {
            return MergeDependencies(currentDependencies, newDependencies, "import", ";");
        }
    }
}
