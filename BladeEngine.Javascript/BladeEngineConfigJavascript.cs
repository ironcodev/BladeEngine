using BladeEngine.Core;
using System.Collections.Generic;

namespace BladeEngine.Javascript
{
    public class BladeEngineRunnerConfig
    {
        public bool CheckNodeJsExistence { get; set; }
    }
    public class BladeEngineConfigJavascript : BladeEngineConfigBase
    {
        public override string FileExtension => ".js";
        public List<string> ClassPath { get; set; }
        public BladeEngineRunnerConfig RunnerConfig { get; set; }
        public BladeEngineConfigJavascript()
        {
            RunnerConfig = new BladeEngineRunnerConfig { };
        }
    }
}
