namespace BladeEngine.Core
{
    public enum OutputMode
    {
        None,
        User,
        Manual,
        Auto
    }
    public class BladeEngineOptions
    {
        public bool Debug { get; set; }
        public string Engine { get; set; }
        public string EngineLibraryPath { get; set; }
        public string InputFile { get; set; }
        public string OutputFile { get; set; }
        public OutputMode OutputMode { get; set; }
        public bool DontOverwriteExistingOutputFile { get; set; }
        public string RunnerOutputFile { get; set; }
        public bool DontOverwriteExistingRunnerOutputFile { get; set; }
        public bool Runner { get; set; }
        public bool PrintRunnerOutput { get; set; }
        public string GivenConfig { get; set; }
        public bool UseConfig { get; set; }
        public bool UseModel { get; set; }
        public string GivenModel { get; set; }
        public string ModelPath { get; set; }
    }
}
