using System.IO;
using BladeEngine.Core.Utils;
using BladeEngine.Core.Utils.Logging;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core
{
    public class BladeOptionsValidateResult : ServiceResponse
    {
    }
    public class BladeRunnerOptions
    {
        public bool Debug { get; set; }
        private string cacheDir;
        public string CacheDir
        {
            get
            {
                if (string.IsNullOrEmpty(cacheDir))
                {
                    cacheDir = AppPath.ProgramDir + "\\cache";
                }

                if (!Path.IsPathRooted(cacheDir))
                {
                    cacheDir = Path.Combine(AppPath.ProgramDir, cacheDir);
                }

                return cacheDir;
            }
            set
            {
                cacheDir = value;
            }
        }
        public string Input { get; set; }
        public string OutputFile { get; set; }
        public bool DontOverwriteExistingOutputFile { get; set; }
        public string RunnerOutputFile { get; set; }
        public bool LogRunnerOutput { get; set; }
        public bool DontOverwriteExistingRunnerOutputFile { get; set; }
        public bool Runner { get; set; }
        public object Model { get; set; }
        public BladeOptionsValidateResult Validate()
        {
            var result = new BladeOptionsValidateResult();

            do
            {
                if (!IsSomeString(Input))
                {
                    result.SetStatus("NoInput");
                    break;
                }

                if (!IsSomeString(OutputFile))
                {
                    result.SetStatus("NoOutputFile");
                    break;
                }

                if (File.Exists(OutputFile) && DontOverwriteExistingOutputFile)
                {
                    result.SetStatus("OutputAlreadyExists");
                    break;
                }

                if (File.Exists(RunnerOutputFile) && DontOverwriteExistingRunnerOutputFile)
                {
                    result.SetStatus("RunnerOutputAlreadyExists");
                    break;
                }

                result.Succeeded = true;
                result.Status = "Succeeded";
            } while (false);
            return result;
        }
    }
}
