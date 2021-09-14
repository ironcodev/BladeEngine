using System;
using System.IO;
using BladeEngine.Core.Utils.Logging;
using Newtonsoft.Json;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core
{
    public abstract class BladeRunner
    {
        public ILogger Logger { get; set; }
        protected BladeEngineOptions Options { get; set; }
        protected BladeTemplateBase Template { get; set; }
        protected string RenderedTemplate { get; set; }
        public abstract BladeEngineConfigBase Config { get; }
        public BladeRunner(ILogger logger, BladeEngineOptions options)
        {
            Logger = logger;
            Options = options;
        }
        public abstract void Run();
        protected void Abort(string message)
        {
            Logger.Abort(message, !Options.Debug);
        }
    }
    public abstract class BladeRunner<TBladeEngine, TBladeEngineConfig>: BladeRunner
        where TBladeEngine: BladeEngineBase, new()
        where TBladeEngineConfig: BladeEngineConfigBase, new()
    {
        public BladeRunner(ILogger logger, BladeEngineOptions options) : base(logger, options)
        { }
        protected TBladeEngine Engine { get; private set; }
        public override BladeEngineConfigBase Config => StrongConfig;
        private TBladeEngineConfig strongConfig;
        public TBladeEngineConfig StrongConfig
        {
            get
            {
                if (strongConfig == null)
                {
                    strongConfig = new TBladeEngineConfig();
                }

                return strongConfig;
            }
            set
            {
                strongConfig = value;
            }
        }
        bool InitConfig(string config)
        {
            var result = true;

            if (IsSomeString(config, true))
            {
                var cfg = Logger.Try("Deserializing engine configuration ...", Options.Debug, () => JsonConvert.DeserializeObject<TBladeEngineConfig>(config));

                StrongConfig = cfg;

                result = cfg != null;
            }
            else
            {
                if (Options.Debug)
                {
                    Logger.Log(Environment.NewLine + "No config is given. Used default config.");
                }
            }

            Engine.Config = StrongConfig;
            
            if (Options.Debug)
            {
                Logger.Log(Environment.NewLine + "Config is:");
                Logger.Debug(Environment.NewLine + JsonConvert.SerializeObject(StrongConfig, Formatting.Indented));
            }

            return result;
        }
        bool GetInput(string inputFile, out string content)
        {
            var _content = "";

            var result = Logger.Try($"Reading input file '{inputFile}' ...", Options.Debug, () =>
            {
                _content = File.ReadAllText(inputFile);

                return true;
            });

            if (!result)
            {
                Abort($"Reading input file '{inputFile}' failed");
            }
            else
            {
                if (string.IsNullOrEmpty(_content))
                {
                    Logger.Warn($"Input file '{inputFile}' is empty.");
                }
            }

            content = _content;

            return result;
        }
        bool Parse(string content)
        {
            Template = Logger.Try($"Parsing input template ...", Options.Debug, () => Engine.Parse(content));

            return Template != null;
        }
        bool Render()
        {
            var renderedTemplate = "";
            var result = Logger.Try($"Rendering template ...", Options.Debug, () =>
            {
                renderedTemplate = Template.Render();

                return true;
            });

            RenderedTemplate = renderedTemplate;

            return result;
        }
        bool WriteOutput()
        {
            var result = false;

            if (Options.Runner && Options.OutputMode == OutputMode.Manual)
            {
                result = true;
            }
            else
            {
                if (Options.OutputMode != OutputMode.None)
                {
                    result = Logger.Try($"Writing output '{Options.OutputFile}' ...", Options.Debug, () => File.WriteAllText(Options.OutputFile, RenderedTemplate));
                }
                else
                {
                    result = true;
                }
            }
            
            return result;
        }
        bool WriteRunnerOutput(string runnerOutput)
        {
            var result = false;

            if (Options.Debug)
            {
                Logger.Debug("Runner output is");
                Logger.Log($"Length: {(runnerOutput?.Length ?? 0)}");
                Logger.Log(runnerOutput);
            }

            result = Logger.Try($"Writing runner output into '{Options.RunnerOutputFile}' ...", Options.Debug, () =>
            {
                File.WriteAllText(Options.RunnerOutputFile, runnerOutput);

                return true;
            });

            if (!result)
            {
                Abort($"Writing runner output into '{Options.RunnerOutputFile}' failed");
            }

            return result;
        }
        protected abstract bool Execute(out string result);
        public override void Run()
        {
            string content;

            if (GetInput(Options.InputFile, out content))
            {
                Engine = new TBladeEngine();

                if (InitConfig(Options.GivenConfig))
                {
                    if (Parse(content))
                    {
                        if (Render())
                        {
                            if (string.IsNullOrEmpty(RenderedTemplate))
                            {
                                Logger.Warn($"'{Options.Engine}' template Render() method did not produce any result.");
                            }
                            else
                            {
                                if (WriteOutput())
                                {
                                    if (Options.Runner)
                                    {
                                        var runnerOutput = "";

                                        if (Logger.Try("Running generated code ...", Options.Debug, () => Execute(out runnerOutput)))
                                        {
                                            if (!Options.Debug)
                                            {
                                                Logger.Log($"Runner succeeded.");
                                            }

                                            if (WriteRunnerOutput(runnerOutput))
                                            {
                                                Logger.Log($"Runner output saved.");
                                            }
                                        }
                                        else
                                        {
                                            if (!Options.Debug)
                                            {
                                                Logger.Warn($"Runner did not succeed. Use -debug for details.");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (Options.OutputMode == OutputMode.Manual)
                                        {
                                            Logger.Warn("Neither output specified nor asked to run the template. What's on your mind?");
                                        }
                                    }
                                }
                                else
                                {
                                    Abort($"Writing output '{Options.OutputFile}' ...");
                                }
                            }
                        }
                        else
                        {
                            Abort("Rendering template failed");
                        }
                    }
                    else
                    {
                        Abort("Parsing input template failed");
                    }
                }
            }
        }
    }
}
