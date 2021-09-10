using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace BladeEngine.Core
{
    public abstract class BladeRunner
    {
        public ILogger Logger { get; set; }
        protected BladeEngineOptions Options { get; set; }
        protected BladeTemplateBase Template { get; set; }
        protected string RenderedTemplate { get; set; }
        public BladeRunner(ILogger logger, BladeEngineOptions options)
        {
            Logger = logger;
            Options = options;
        }
        public abstract void Run();
        protected void Debug(string message)
        {
            if (Options.Debug)
            {
                Logger.DebugLn(message);
            }
        }
    }
    public abstract class BladeRunner<TBladeEngine, TBladeEngineConfig>: BladeRunner
        where TBladeEngine: BladeEngineBase, new()
        where TBladeEngineConfig: BladeEngineConfigBase, new()
    {
        public BladeRunner(ILogger logger, BladeEngineOptions options) : base(logger, options)
        { }
        protected TBladeEngine Engine { get; private set; }
        protected TBladeEngineConfig Config { get; private set; }
        bool InitConfig(string config)
        {
            var result = true;

            Debug("Initializing engine configuration ...");

            if (!string.IsNullOrEmpty(config))
            {
                try
                {
                    Config = JsonConvert.DeserializeObject<TBladeEngineConfig>(config);

                    if (Options.Debug)
                    {
                        Logger.SuccessLn("Done");
                    }
                }
                catch (Exception e)
                {
                    Logger.LogLn("Error deserializing config");
                    Logger.Log(e);

                    result = false;
                }
            }
            else
            {
                Debug("No config is given. Used default config.");

                Config = new TBladeEngineConfig();
            }

            Engine.Config = Config;
            
            if (Options.Debug)
            {
                Logger.LogLn("Config is");
                Logger.LogLn(Environment.NewLine + JsonConvert.SerializeObject(Config, Formatting.Indented) + Environment.NewLine);
            }

            return result;
        }
        bool GetInput(string inputFile, out string content)
        {
            var result = false;

            content = "";

            Debug($"Reading input file {inputFile} ...");

            if (!File.Exists(inputFile))
            {
                Logger.LogLn($"Input file {inputFile} does not exist.");
            }
            else
            {
                try
                {
                    content = File.ReadAllText(inputFile);

                    if (string.IsNullOrEmpty(content))
                    {
                        Logger.WarnLn($"Input file '{inputFile}' is empty.");
                    }
                    else
                    {
                        result = true;

                        if (Options.Debug)
                        {
                            Logger.SuccessLn("Done");
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogLn($"Error reading input file {inputFile}");
                    Logger.Log(e);
                }
            }

            return result;
        }
        bool Parse(string content)
        {
            var result = false;

            Debug("Parsing input template ...");

            try
            {
                Template = Engine.Parse(content);

                result = true;

                if (Options.Debug)
                {
                    Logger.SuccessLn("Done");
                }
            }
            catch (Exception e)
            {
                Logger.LogLn($"Error parsing input template.");
                Logger.Log(e);
            }

            return result;
        }
        bool Render()
        {
            var result = false;

            Debug("Rendering template ...");

            try
            {
                RenderedTemplate = Template.Render();

                result = true;

                if (Options.Debug)
                {
                    Logger.SuccessLn("Done");
                }
            }
            catch (Exception e)
            {
                Logger.LogLn($"Error while rendering template.");
                Logger.Log(e);
            }

            return result;
        }
        bool WriteOutput(string outputFile)
        {
            var result = false;

            Debug($"Writing output {outputFile} ...");
            Debug($"Output length = {RenderedTemplate.Length}");

            try
            {
                File.WriteAllText(outputFile, RenderedTemplate);

                result = true;

                if (Options.Debug)
                {
                    Logger.SuccessLn("Done");
                }
            }
            catch (Exception e)
            {
                Logger.LogLn($"Error while writing output.");
                Logger.Log(e);
            }

            return result;
        }
        void WriteRunnerOutput(string outputFile, string output)
        {
            Debug($"Saving runner output '{outputFile}' ...");

            try
            {
                File.WriteAllText(outputFile, output);

                if (Options.Debug)
                {
                    Logger.SuccessLn("Done");
                }
            }
            catch (Exception e)
            {
                Logger.LogLn($"Error while writing output.");
                Logger.Log(e);
            }
        }
        protected abstract string Execute();
        public override void Run()
        {
            string content;

            Debug($"Requested engine is '{Options.Engine}'");

            if (GetInput(Options.InputFile, out content))
            {
                Engine = new TBladeEngine();

                if (InitConfig(Options.GivenConfig))
                {
                    if (Parse(content))
                    {
                        if (Render())
                        {
                            if (WriteOutput(Options.OutputFile))
                            {
                                if (Options.Runner)
                                {
                                    Debug("Running generated code ...");

                                    try
                                    {
                                        var result = Execute();

                                        WriteRunnerOutput(Options.RunnerOutputFile, result);
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.LogLn($"Error running generated code.");
                                        Logger.Log(e);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
