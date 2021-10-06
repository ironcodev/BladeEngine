using System;

namespace BladeEngine.Core
{
    public class BladeTemplateSettings
    {
        public string Path { get; set; }
        public string AbsolutePath { get; set; }
        public bool IsLocal { get; set; }
        public bool IsInclude { get; set; }
        public BladeTemplateSettings()
        {
            Path = ".";
            AbsolutePath = Environment.CurrentDirectory;
            IsLocal = true;
        }
    }
}
