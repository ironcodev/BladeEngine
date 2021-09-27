using System;
using System.Text;
using BladeEngine.Core;
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
        protected override BladeTemplateBase CreateTemplate(string path)
        {
            return new BladeTemplateJava(this, path);
        }
        protected override string WriteValue(string str)
        {
            return $"{Environment.NewLine}_buffer.append({str});";
        }
        protected override string MergeDependencies(string currentDependencies, string newDependencies)
        {
            return MergeDependencies(currentDependencies, newDependencies, "import", ";");
        }

        protected override void OnIncludeTemplate(BladeTemplateBase current, BladeTemplateBase include)
        {
            var includePackage = "";

            if (include.Path.StartsWith(".."))
            includePackage = include.Path.Substring(current.Path.Length).Replace("/", ".").Replace("\\", ".");

            if (includePackage.StartsWith("."))
            {
                includePackage = includePackage.Substring(1);
            }

            current.Dependencies += Environment.NewLine + "import " + includePackage + "." + include.GetMainClassName() + ";";
        }
    }
}
