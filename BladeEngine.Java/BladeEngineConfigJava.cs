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
        private string package;
        public string Package
        {
            get
            {
                if (string.IsNullOrEmpty(package))
                {
                    package = "Blade";
                }

                return package;
            }
            set
            {
                package = value;
            }
        }
        public List<string> ClassPath { get; set; }
        public BladeEngineRunnerConfig RunnerConfig { get; set; }
        public BladeEngineConfigJava()
        {
            RunnerConfig = new BladeEngineRunnerConfig { };
        }
    }
}
