using BladeEngine.Core.Utils;

namespace BladeEngine.Core
{
    public class BladeRunnerRunResult : ServiceResponse
    {
        public BladeTemplateBase Template { get; set; }
        public bool ParseSuceeded { get; set; }
        public bool RenderSuceeded { get; set; }
        public bool RunnerSuceeded { get; set; }
        public bool SaveOutputSuceeded { get; set; }
        public bool SaveRunnerOutputSuceeded { get; set; }
        public string RenderedTemplate { get; set; }
        public string RunnerOutput { get; set; }
    }
}
