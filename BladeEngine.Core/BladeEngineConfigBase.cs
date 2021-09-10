namespace BladeEngine.Core
{
    public class BladeEngineConfigBase
    {
        public bool CamelCase { get; set; }
        public bool SkipExcessiveNewLines { get; set; }
        public BladeEngineConfigBase()
        {
            SkipExcessiveNewLines = true;
        }
    }
}
