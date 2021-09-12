using System;
using System.Diagnostics;
using System.IO;
using BladeEngine.Core;
using BladeEngine.Core.Utils;

namespace BladeEngine.Java
{
    public class BladeRunnerJava: BladeRunner<BladeEngineJava, BladeEngineConfigJava>
    {
        public BladeRunnerJava(ILogger logger, BladeEngineOptions options) : base(logger, options)
        { }

        protected override bool Execute(out string result)
        {
            var program = $@"
import {Template.GetMainClassName()};

public class Program{Guid.NewGuid().ToString().Replace("-", "")} {{
    public static void main(String[] args) {{
        {Template.GetMainClassName()} t = new {Template.GetMainClassName()}();

        System.out.println(t.render());
    }}
}}
";
            var sr = Shell.Execute(new ShellExecuteRequest { FileName = "javac.exe", Args = Path.GetFileName(Options.OutputFile), WorkingDirectory = Environment.CurrentDirectory });

            result =  sr.Output;

            return sr.Succeeded;
        }
    }
}
