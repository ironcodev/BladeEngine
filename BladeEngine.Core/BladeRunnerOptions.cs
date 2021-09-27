using System;
using System.IO;
using BladeEngine.Core.Utils;
using BladeEngine.Core.Utils.Logging;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core
{
    public enum OutputMode
    {
        None,
        User,
        Manual,
        Auto
    }
    public class BladeRunnerConfigValidateResult: ServiceResponse
    {
    }
    public class BladeRunnerOptions
    {
        public bool Debug { get; set; }
        public string DefaultOutputExtensions { get; set; }
        public string EngineName { get; set; }
        public string EngineLibraryPath { get; set; }
        private string cacheDir;
        public string CacheDir
        {
            get
            {
                if (string.IsNullOrEmpty(cacheDir))
                {
                    cacheDir = AppPath.ExecDir + "\\cache";
                }

                if (!Path.IsPathRooted(cacheDir))
                {
                    cacheDir = Path.Combine(AppPath.ExecDir, cacheDir);
                }

                return cacheDir;
            }
            set
            {
                cacheDir = value;
            }
        }
        public string Input { get; set; }
        public string InputFile { get; set; }
        internal string _Input { get; set; }
        public string OutputFile { get; set; }
        public OutputMode OutputMode { get; set; }
        public bool DontOverwriteExistingOutputFile { get; set; }
        public string RunnerOutputFile { get; set; }
        public OutputMode RunnerOutputMode { get; set; }
        public bool DontOverwriteExistingRunnerOutputFile { get; set; }
        public bool Runner { get; set; }
        public bool LogRunnerOutput { get; set; }
        public string GivenConfig { get; set; }
        public bool UseConfig { get; set; }
        public bool UseModel { get; set; }
        public string GivenModel { get; set; }
        public string ModelPath { get; set; }
        public object Config { get; set; }
        public object Model { get; set; }
        private bool ValidateInput(ILogger logger, BladeRunnerConfigValidateResult validateResult)
        {
            var result = false;
            Exception ex;

            do
            {
                if (string.IsNullOrEmpty(InputFile) && string.IsNullOrEmpty(Input))
                {
                    validateResult.SetStatus("NoInputSpecified");
                    logger.Log("No input template is specified to be compiled.");
                    break;
                }

                if (IsSomeString(Input))
                {
                    _Input = Input;
                }
                else
                {
                    if (!Path.IsPathRooted(InputFile))
                    {
                        InputFile = Path.Combine(Environment.CurrentDirectory, InputFile);
                    }

                    if (!File.Exists(InputFile))
                    {
                        validateResult.SetStatus("InputFileNotFound");
                        logger.Log($"input file '{InputFile}' does not exist.");
                        break;
                    }

                    if (!logger.Try($"Reading input file '{InputFile}' ...", Debug, () =>
                    {
                        _Input = File.ReadAllText(InputFile);

                        return true;
                    }, out ex))
                    {
                        validateResult.SetStatus("ReadingInputFileFailed");
                        validateResult.Exception = ex;
                        logger.Abort($"Reading input file '{InputFile}' failed", !Debug);
                        break;
                    }

                    if (string.IsNullOrEmpty(_Input))
                    {
                        validateResult.SetStatus("InputFileEmpty");
                        logger.Warn($"Input file '{InputFile}' is empty.");
                        break;
                    }
                }

                validateResult.SetStatus("Succeeded");
                result = true;
            } while (false);

            return result;
        }
        private bool ValidateConfig(ILogger logger, BladeRunnerConfigValidateResult validateResult)
        {
            var result = false;

            do
            {
                if (IsSomeString(GivenConfig))
                {
                    GivenConfig = GivenConfig.Trim();

                    if (!(GivenConfig.StartsWith("{") && GivenConfig.EndsWith("}")))  // if config is not json, assume it to be a filepath
                    {
                        var configPath = Path.IsPathRooted(GivenConfig) ? GivenConfig : Path.Combine(Environment.CurrentDirectory, GivenConfig);

                        if (!File.Exists(configPath))
                        {
                            validateResult.SetStatus("ConfigFileNotFound");
                            logger.Log($"config file '{GivenConfig}' not found.");
                            break;
                        }

                        if (!logger.Try($"Reading config file {configPath}", Debug, () =>
                        {
                            GivenConfig = File.ReadAllText(configPath);
                        }))
                        {
                            validateResult.SetStatus("ReadingConfigFailed");
                            logger.Abort($"Reading config file '{configPath}' failed", !Debug);
                            break;
                        }
                    }
                }
                else
                {
                    if (UseConfig)
                    {
                        validateResult.SetStatus("NoConfigGiven");
                        logger.Log($"-c is used but no config is given");
                        break;
                    }
                }

                result = true;
            } while (false);

            return result;
        }
        private bool ValidateOutput(ILogger logger, BladeRunnerConfigValidateResult validateResult)
        {
            var result = false;

            do
            {
                if (IsSomeString(OutputFile))
                {
                    if (!Path.IsPathRooted(OutputFile))
                    {
                        OutputFile = Path.Combine(Environment.CurrentDirectory, OutputFile);
                    }

                    if (File.Exists(OutputFile) && DontOverwriteExistingOutputFile)
                    {
                        validateResult.SetStatus("OutputAlreadyExists");
                        logger.Log($"output file '{OutputFile}' already exists.");
                        break;
                    }
                }
                else
                {
                    var filename = Path.GetFileNameWithoutExtension(InputFile);

                    OutputFile = Path.Combine(Path.GetDirectoryName(InputFile), filename + DefaultOutputExtensions);

                    if (OutputMode != OutputMode.Auto && OutputMode != OutputMode.User)
                    {
                        OutputMode = OutputMode.Manual;
                    }
                }

                result = true;
            } while (false);

            return result;
        }
        private bool ValidateRunner(ILogger logger, BladeRunnerConfigValidateResult validateResult)
        {
            var result = false;
            Exception ex;

            do
            {
                if (Runner)
                {
                    // STEP 4.1. Validate runner output

                    if (IsSomeString(RunnerOutputFile))
                    {
                        if (!Path.IsPathRooted(RunnerOutputFile))
                        {
                            RunnerOutputFile = Path.Combine(Environment.CurrentDirectory, RunnerOutputFile);
                        }

                        if (File.Exists(RunnerOutputFile) && DontOverwriteExistingRunnerOutputFile)
                        {
                            validateResult.SetStatus("RunnerOutputAlreadyExists");
                            logger.Log($"runner output file '{RunnerOutputFile}' already exist.");
                            break;
                        }
                    }
                    else
                    {
                        var filename = Path.GetFileNameWithoutExtension(InputFile);

                        RunnerOutputFile = Path.Combine(Environment.CurrentDirectory, filename + ".output");

                        if (RunnerOutputMode != OutputMode.Auto && RunnerOutputMode != OutputMode.User)
                        {
                            RunnerOutputMode = OutputMode.Manual;
                        }
                    }

                    // STEP 4.2. Validate runner model

                    if (IsSomeString(GivenModel))
                    {
                        GivenModel = GivenModel.Trim();

                        if (!(GivenModel.StartsWith("{") && GivenModel.EndsWith("}")))  // if model is not json, assume it as a file
                        {
                            ModelPath = Path.IsPathRooted(GivenModel) ? GivenModel : Path.Combine(Environment.CurrentDirectory, GivenModel);

                            if (!File.Exists(ModelPath))
                            {
                                validateResult.SetStatus("ModelFileNotFound");
                                logger.Log($"model file '{ModelPath}' not found.");
                                break;
                            }

                            if (!logger.Try($"Reading model file {ModelPath}", Debug, () =>
                            {
                                GivenModel = File.ReadAllText(ModelPath);
                            }, out ex))
                            {
                                validateResult.SetStatus("ReadingModelFailed");
                                logger.Abort($"Reading model file '{ModelPath}' failed", !Debug);
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (UseModel)
                        {
                            validateResult.SetStatus("NoModelSpecified");
                            logger.Log($"-m is used but no model is given");
                            break;
                        }
                    }
                }
                else
                {
                    RunnerOutputFile = "";
                }

                if (!Runner && LogRunnerOutput)
                {
                    validateResult.SetStatus("LogRunnerOutputRequiresSettingRunnerToTrue");
                    logger.Log($"Please add 'runner' switch as well in order to print runner output");
                    break;
                }

                result = true;
            } while (false);

            return result;
        }
        public virtual BladeRunnerConfigValidateResult Validate(ILogger logger)
        {
            var result = new BladeRunnerConfigValidateResult();
            
            do
            {
                // STEP 1. Validate input template
                if (!ValidateInput(logger, result))
                {
                    break;
                }

                // STEP 2. Validate given engine config
                if (!ValidateConfig(logger, result))
                {
                    break;
                }

                // STEP 3. Validate output
                if (!ValidateOutput(logger, result))
                {
                    break;
                }

                // STEP 4. Validate runner
                if (!ValidateRunner(logger, result))
                {
                    break;
                }

                result.Succeeded = true;
                result.Status = "Succeeded";
            } while (false);


            return result;
        }
    }
    public class BladeRunnerConfig<TConfig>: BladeRunnerOptions
        where TConfig: BladeEngineConfigBase
    {
        public TConfig StrongConfig
        {
            get
            {
                return (TConfig)Config;
            }
            set { Config = value; }
        }
    }
}
