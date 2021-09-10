using System;
using BladeEngine.Core;

namespace BladeEngine.Java
{
    public class BladeRunnerJava: BladeRunner<BladeEngineJava, BladeEngineConfigJava>
    {
        public BladeRunnerJava(ILogger logger, BladeEngineOptions options) : base(logger, options)
        { }

        protected override string Execute()
        {
            throw new NotImplementedException();
        }
    }
}
