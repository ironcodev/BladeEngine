using System;
using System.IO;
using BladeEngine.Core.Utils.Logging;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core
{
    public abstract class BladeRunner
    {
        public ILogger Logger { get; set; }
        public abstract BladeEngineBase Engine { get; }
        public BladeRunner(ILogger logger)
        {
            Logger = logger;
        }
        public abstract BladeRunnerRunResult Run(BladeRunnerOptions options);
    }
    public abstract class BladeRunner<TBladeEngine, TBladeEngineConfig> : BladeRunner
        where TBladeEngine : BladeEngineBase, new()
        where TBladeEngineConfig : BladeEngineConfigBase, new()
    {
        public BladeRunner(ILogger logger) : base(logger)
        {
            StrongEngine = new TBladeEngine();
        }
        public override BladeEngineBase Engine => StrongEngine;
        public TBladeEngine StrongEngine { get; private set; }
        bool Parse(BladeRunnerOptions options, out BladeTemplateBase template, out Exception ex)
        {
            template = Logger.Try($"Parsing input template ...", options.Debug, () => Engine.Parse(options.Input), out ex);

            return template != null;
        }
        bool Render(BladeRunnerOptions options, BladeTemplateBase template, out string renderedTemplate, out Exception ex)
        {
            var _renderedTemplate = "";
            var result = Logger.Try($"Rendering template ...", options.Debug, () =>
            {
                _renderedTemplate = template.Render();

                return true;
            }, out ex);

            renderedTemplate = _renderedTemplate;

            return result;
        }
        bool SaveOutput(BladeRunnerOptions options, string renderedTemplate, out Exception ex)
        {
            var result = false;

            ex = null;

            if (!IsSomeString(options.OutputFile))
            {
                result = true;
            }
            else
            {
                result = Logger.Try($"Writing output '{options.OutputFile}' ...", options.Debug, () => File.WriteAllText(options.OutputFile, renderedTemplate), out ex);

                if (!options.LogRunnerOutput)
                {
                    if (result)
                    {
                        Logger.Log($"Output '{options.OutputFile}' created.", !options.Debug);
                    }
                    else
                    {
                        Logger.Abort($"Writing output '{options.OutputFile}' failed", !options.Debug);
                    }
                }
            }

            return result;
        }
        bool SaveRunnerOutput(BladeRunnerOptions options, string runnerOutput, out Exception ex)
        {
            var result = false;

            ex = null;

            if (IsSomeString(options.RunnerOutputFile))
            {
                if (options.Debug)
                {
                    Logger.Debug("Runner output is");
                    Logger.Log($"Length: {(runnerOutput?.Length ?? 0)}");
                    Logger.Log(runnerOutput);
                }

                result = Logger.Try($"Saving runner output '{options.RunnerOutputFile}' ...", options.Debug, () =>
                {
                    File.WriteAllText(options.RunnerOutputFile, runnerOutput);

                    return true;
                }, out ex);

                if (result)
                {
                    if (!options.Debug && !options.LogRunnerOutput)
                    {
                        Logger.Log($"Runner output '{options.RunnerOutputFile}' created.");
                    }
                }
                else
                {
                    Logger.Abort($"Saving runner output '{options.RunnerOutputFile}' failed", !options.Debug);
                }
            }
            else
            {
                result = true;
            }

            return result;
        }
        protected abstract bool Execute(BladeRunnerOptions options, BladeRunnerRunResult runnerResult, out string result);
        bool Runner(BladeRunnerOptions options, BladeRunnerRunResult runnerResult, out string runnerOutput, out Exception ex)
        {
            var result = false;
            var _runnerOutput = "";

            ex = null;

            result = Logger.Try("Running generated code ...", options.Debug, () => Execute(options, runnerResult, out _runnerOutput), out ex);

            if (result)
            {
                if (options.LogRunnerOutput)
                {
                    Logger.Log(_runnerOutput);
                }
                else
                {
                    if (!options.Debug)
                    {
                        Logger.Log($"Runner succeeded.");
                    }
                }
            }
            else
            {
                Logger.Abort($"Runner did not succeed", !options.Debug);
            }

            runnerOutput = _runnerOutput;

            return result;
        }
        public override BladeRunnerRunResult Run(BladeRunnerOptions options)
        {
            var result = new BladeRunnerRunResult();
            Exception ex = null;

            do
            {
                var validateResult = options.Validate();

                if (!validateResult.Succeeded)
                {
                    result.Copy(validateResult);
                    break;
                }

                BladeTemplateBase template;

                if (!Parse(options, out template, out ex))
                {
                    result.TrySetStatus("ParsingTemplateFailed");
                    Logger.Abort("Parsing input template failed", !options.Debug);
                    break;
                }

                result.ParseSuceeded = true;
                result.Template = template;

                string renderedTemplate;

                if (!Render(options, template, out renderedTemplate, out ex))
                {
                    result.TrySetStatus("RenderingTemplateFailed");
                    Logger.Abort("Rendering template failed", !options.Debug);
                    break;
                }

                result.RenderSuceeded = true;
                result.RenderedTemplate = renderedTemplate;

                if (!IsSomeString(renderedTemplate))
                {
                    result.TrySetStatus("TemplateRenderHadNoResult");
                    Logger.Warn($"Rendering template did not produce any result. Running template aborted.");
                    break;
                }

                if (!SaveOutput(options, renderedTemplate, out ex))
                {
                    result.TrySetStatus("SaveOutputFailed");
                    Logger.Abort($"Writing output '{options.OutputFile}' ...", !options.Debug);
                    break;
                }

                result.SaveOutputSuceeded = true;

                if (options.Runner)
                {
                    string runnerOutput;

                    result.RunnerSuceeded = Runner(options, result, out runnerOutput, out ex);
                    
                    if (!result.RunnerSuceeded)
                    {
                        if (IsSomeString(options.RunnerOutputFile))
                        {
                            result.TrySetStatus("RunnerFailed");
                            Logger.Warn("Neither output specified nor asked to run the template. What's on your mind?");
                        }

                        break;
                    }
                    else
                    {
                        result.RunnerOutput = runnerOutput;
                        result.SaveRunnerOutputSuceeded = SaveRunnerOutput(options, runnerOutput, out ex);
                        result.TrySetStatus("RunnerSucceeded");
                    }
                }

            } while (false);

            result.Exception = ex;

            return result;
        }
    }
}
