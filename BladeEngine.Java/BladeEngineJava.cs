using System;
using System.Text;
using BladeEngine.Core;
using BladeEngine.Core.Utils;
using static BladeEngine.Core.Utils.LanguageConstructs;

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
            var result = new StringBuilder();

            if (IsSomeString(str))
            {
                var lines = str.Split(Environment.NewLine);
                var i = 0;

                foreach (var line in lines)
                {
                    result.Append($"{Environment.NewLine}_buffer.append(\"{line.Replace("\"", "\"\"")}{(i++ < lines.Length - 1 ? "\\n": "")}\");");
                }
            }

            return result.ToString();
        }
        protected override BladeTemplateBase CreateTemplate(BladeTemplateSettings settings)
        {
            return new BladeTemplateJava(this, settings);
        }
        protected override string WriteValue(string str)
        {
            return $"{Environment.NewLine}_buffer.append({str});";
        }
        protected override string MergeDependencies(string currentDependencies, string newDependencies)
        {
            return MergeDependencies(currentDependencies, newDependencies, "import", ";");
        }
        protected override bool OnIncludeTemplate(CharReader reader, BladeTemplateBase current, BladeTemplateBase include)
        {
            current.Dependencies += Environment.NewLine + $"import {include.GetFullMainClassName()};";

            return true;
        }
    }
}
