using BladeEngine.Core;
using System.Collections.Generic;

namespace BladeEngine.Java
{
    public class BladeEngineRunnerConfig
    {
        public bool CheckJdkExistence { get; set; }
    }
    public class BladeEngineConfigJava : BladeEngineConfigBase
    {
        public override string FileExtension => ".java";
        public List<string> ClassPath { get; set; }
        public BladeEngineRunnerConfig RunnerConfig { get; set; }
        public BladeEngineConfigJava()
        {
            RunnerConfig = new BladeEngineRunnerConfig { };
        }
    }
}
