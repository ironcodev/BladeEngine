namespace BladeEngine.Core
{
    public abstract class BladeEngineConfigBase
    {
        public abstract string FileExtension { get; }
        public bool CamelCase { get; set; }
        public bool SkipExcessiveNewLines { get; set; }
        public BladeEngineConfigBase()
        {
            SkipExcessiveNewLines = true;
        }
    }
}
