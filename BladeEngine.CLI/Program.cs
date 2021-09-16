using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BladeEngine.Core;
using BladeEngine.Core.Utils;
using BladeEngine.Core.Utils.Logging;
using Newtonsoft.Json;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.CLI
{
    class Program
    {
        static string Version => "1.0.0";
        static ILogger logger => new ConsoleLogger();
        static void Help()
        {
            Console.WriteLine($@"
Blade Template Engine v{Version}

blade [runner] [-e engine] [-c engine-config] [-i input-template] [-o output] [-on]
      [-r runner-output] [-rn] [pr] [-m model] [-debug] [-v] [-?]

options:
    runner  :   run template
    -e      :   specify blade engine to parse the template. internal engines are:
                    csharp, java, javascript, python
    -i      :   specify input balde template
    -o      :   specify filename to save generated content into. if no filename is specified, use automatic filename
    -on     :   do not overwrite output if already exists
    -r      :   in case of using 'runner', a filename to save the result of executing generated code
    -rn     :   do not overwrite runner output if already exists
    -pr     :   print runner output
    -c      :   engine config in json format or a filename that contains engine config in json format
    -m      :   model in json format or a filename containing model in json format
    -debug  :   execute runner in debug mode
    -v      :   program version
    -?      :   show this help

example:
    blade -e csharp -i my-template.blade
    blade -e csharp -i my-template.blade -o my-template.cs
    blade -e csharp -i my-template.blade -c ""{{ 'NameSpace': 'MyNS' }}""
    blade runner -e csharp -i my-template.blade -m ""{{ 'name': 'John Doe' }}""
");
        }
        static string GetAssemblyPath(string arg)
        {
            string result;

            do
            {
                result = Path.Combine(Environment.CurrentDirectory, ".\\BladeEngine." + arg + ".dll");

                if (File.Exists(result))
                {
                    break;
                }

                result = Path.Combine(AppPath.ExecDir, ".\\BladeEngine." + arg + ".dll");

                if (File.Exists(result))
                {
                    break;
                }

                result = Path.Combine(Environment.CurrentDirectory, ".\\" + arg + ".dll");

                if (File.Exists(result))
                {
                    break;
                }

                result = Path.Combine(AppPath.ExecDir, ".\\" + arg + ".dll");

                if (File.Exists(result))
                {
                    break;
                }

                result = null;
            } while (false);

            return result;
        }
        static bool Validate(BladeEngineOptions options, out BladeRunner runner)
        {
            var result = false;

            runner = null;

            do
            {
                // STEP 1. Validate Egnine and extract its runner

                if (IsSomeString(options.EngineLibraryPath))
                {
                    var assembly = logger.Try($"Loading Engine assembly '{options.EngineLibraryPath}' ...", options.Debug, () => Assembly.LoadFrom(options.EngineLibraryPath));

                    if (assembly != null)
                    {
                        var runnerType = logger.Try($"Finding runner ...", options.Debug, () => assembly.GetTypes().FirstOrDefault(t => t.DescendsFrom(typeof(BladeRunner))));

                        if (runnerType == null)
                        {
                            logger.Log($"Could not find a runner in '{options.Engine}' engine assembly that is derived from BladeRunner base class.");
                            break;
                        }
                        else
                        {
                            runner = logger.Try($"Instantiating runner ...", options.Debug, () => (BladeRunner)Activator.CreateInstance(runnerType, logger, options));

                            if (runner == null)
                            {
                                logger.Abort($"Instantiating '{options.Engine}' runner failed", !options.Debug);
                                break;
                            }
                        }
                    }
                    else
                    {
                        logger.Abort($"Loading '{options.Engine}' engine assembly failed", !options.Debug);
                    }
                }
                else
                {
                    logger.Log("No engine specified or specified engine is invalid.");
                    break;
                }

                // STEP 2. Validate input template

                if (string.IsNullOrEmpty(options.InputFile))
                {
                    logger.Log("No input template is specified to be compiled.");
                    break;
                }

                if (!Path.IsPathRooted(options.InputFile))
                {
                    options.InputFile = Path.Combine(Environment.CurrentDirectory, options.InputFile);
                }

                if (!File.Exists(options.InputFile))
                {
                    logger.Log($"input file '{options.InputFile}' does not exist.");
                    break;
                }

                // STEP 3. Validate Config

                if (IsSomeString(options.GivenConfig))
                {
                    options.GivenConfig = options.GivenConfig.Trim();

                    if (!(options.GivenConfig.StartsWith("{") && options.GivenConfig.EndsWith("}")))  // if config is not json, assume it as a file
                    {
                        var configPath = Path.IsPathRooted(options.GivenConfig) ? options.GivenConfig : Path.Combine(Environment.CurrentDirectory, options.GivenConfig);

                        if (!File.Exists(configPath))
                        {
                            logger.Log($"config file '{options.GivenConfig}' not found.");
                            break;
                        }

                        if (!logger.Try($"Reading config file {configPath}", options.Debug, () =>
                        {
                            options.GivenConfig = File.ReadAllText(configPath);
                        }))
                        {
                            logger.Abort($"Reading config file '{configPath}' failed", !options.Debug);
                            break;
                        }
                    }
                }
                else
                {
                    if (options.UseConfig)
                    {
                        logger.Log($"-c is used but no config is given");
                        break;
                    }
                }

                // STEP 3. Validate Output

                if (IsSomeString(options.OutputFile))
                {
                    if (!Path.IsPathRooted(options.OutputFile))
                    {
                        options.OutputFile = Path.Combine(Environment.CurrentDirectory, options.OutputFile);
                    }

                    if (File.Exists(options.OutputFile) && options.DontOverwriteExistingOutputFile)
                    {
                        logger.Log($"output file '{options.OutputFile}' already exists.");
                        break;
                    }
                }
                else
                {
                    var filename = Path.GetFileNameWithoutExtension(options.InputFile);

                    options.OutputFile = Path.Combine(Path.GetDirectoryName(options.InputFile), filename + runner.Config.FileExtension);

                    if (options.OutputMode != OutputMode.Auto)
                    {
                        options.OutputMode = OutputMode.Manual;
                    }
                }

                // STEP 4. Validate runner

                if (options.Runner)
                {
                    // STEP 4.1. Validate runner output

                    if (IsSomeString(options.RunnerOutputFile))
                    {
                        if (!Path.IsPathRooted(options.RunnerOutputFile))
                        {
                            options.RunnerOutputFile = Path.Combine(Environment.CurrentDirectory, options.RunnerOutputFile);
                        }

                        if (File.Exists(options.RunnerOutputFile) && options.DontOverwriteExistingRunnerOutputFile)
                        {
                            logger.Log($"runner output file '{options.RunnerOutputFile}' already exist.");
                            break;
                        }
                    }
                    else
                    {
                        var filename = Path.GetFileNameWithoutExtension(options.InputFile);

                        options.RunnerOutputFile = Path.Combine(Environment.CurrentDirectory, filename + ".output");
                    }

                    // STEP 4.2. Validate runner model

                    if (IsSomeString(options.GivenModel))
                    {
                        options.GivenModel = options.GivenModel.Trim();

                        if (!(options.GivenModel.StartsWith("{") && options.GivenModel.EndsWith("}")))  // if model is not json, assume it as a file
                        {
                            options.ModelPath = Path.IsPathRooted(options.GivenModel) ? options.GivenModel : Path.Combine(Environment.CurrentDirectory, options.GivenModel);

                            if (!File.Exists(options.ModelPath))
                            {
                                logger.Log($"model file '{options.ModelPath}' not found.");
                                break;
                            }

                            if (!logger.Try($"Reading model file {options.ModelPath}", options.Debug, () =>
                            {
                                options.GivenModel = File.ReadAllText(options.ModelPath);
                            }))
                            {
                                logger.Abort($"Reading model file '{options.ModelPath}' failed", !options.Debug);
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (options.UseModel)
                        {
                            logger.Log($"-m is used but no model is given");
                            break;
                        }
                    }
                }
                else
                {
                    options.RunnerOutputFile = "";
                }

                result = true;
            } while (false);

            return result;
        }
        static BladeEngineOptions GetOptions(string[] args)
        {
            var result = new BladeEngineOptions();

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg == "-v")
                {
                    logger.Log($"{Environment.NewLine}Blade Template Engine {Version}{Environment.NewLine}");
                    return null;
                }

                if (arg == "-?" || arg == "/?")
                {
                    Help();
                    return null;
                }

                if (arg == "runner")
                {
                    result.Runner = true;
                    continue;
                }

                if (arg == "-debug")
                {
                    result.Debug = true;
                    continue;
                }

                if (arg == "-on")
                {
                    result.DontOverwriteExistingOutputFile = true;
                    continue;
                }

                if (arg == "-rn")
                {
                    result.DontOverwriteExistingRunnerOutputFile = true;
                    continue;
                }

                if (arg == "-pr")
                {
                    result.PrintRunnerOutput = true;
                    continue;
                }

                if (arg == "-i")
                {
                    if (i < args.Length - 1)
                    {
                        result.InputFile = args[i + 1];
                    }

                    continue;
                }

                if (arg == "-o")
                {
                    if (i < args.Length - 1 && !args[i + 1].StartsWith("-") && string.Compare(args[i + 1], "runner", true) != 0 && string.Compare(args[i + 1], "/?", true) != 0 && !IsSomeString(args[i + 1], true))
                    {
                        result.OutputFile = args[i + 1];
                        result.OutputMode = OutputMode.User;
                    }
                    else
                    {
                        result.OutputMode = OutputMode.Auto;
                    }

                    continue;
                }

                if (arg == "-r")
                {
                    if (i < args.Length - 1)
                    {
                        result.RunnerOutputFile = args[i + 1];
                    }

                    continue;
                }

                if (arg == "-c")
                {
                    result.UseConfig = true;

                    if (i < args.Length - 1)
                    {
                        result.GivenConfig = args[i + 1];
                    }

                    continue;
                }

                if (arg == "-m")
                {
                    result.UseModel = true;

                    if (i < args.Length - 1)
                    {
                        result.GivenModel = args[i + 1];
                    }

                    continue;
                }

                if (arg == "-e")
                {
                    if (i < args.Length - 1)
                    {
                        var path = GetAssemblyPath(args[i + 1]);

                        if (IsSomeString(path))
                        {
                            result.EngineLibraryPath = path;
                            result.Engine = args[i + 1].ToLower();
                        }
                    }

                    continue;
                }
                
            }

            return result;
        }
        static void Start(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Help();
            }
            else
            {
                var options = GetOptions(args);

                BladeRunner runner;

                if (options != null && Validate(options, out runner))
                {
                    if (options.Debug)
                    {
                        logger.Log(Environment.NewLine + "Given options:");
                        logger.Debug(Environment.NewLine + JsonConvert.SerializeObject(options, Formatting.Indented) + Environment.NewLine);
                    }

                    runner.Run();
                }
            }
        }
        static void Main(string[] args)
        {
            Start(args);
        }
    }
}
