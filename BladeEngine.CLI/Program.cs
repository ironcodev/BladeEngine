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
        static string Version => "1.0.1";
        static ILogger logger => new ConsoleLogger();
        static void Help()
        {
            Console.WriteLine($@"
Blade Template Engine v{Version}

blade [runner] [-e engine] [-c engine-config] [-i input-template] [-o output] [-on]
      [-r runner-output] [-rn] [pr] [-m model] [-ch cache-path] [-debug] [-v] [-?]

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
    -ch     :   path of a cache dir where runners store their compilation stuff in it (defaul is %AppData%\blade\cache)
    -m      :   model in json format or a filename containing model in json format
    -debug  :   execute runner in debug mode
    -v      :   program version
    -?      :   show this help

example:
    blade -e csharp -i my-template.blade
    blade -e csharp -i my-template.blade -o my-template.cs
    blade -e csharp -i my-template.blade -c ""{{ 'UseStrongModel': true, 'StrongModelType': 'Dictionary<string, object>' }}""
    blade runner -e csharp -i my-template.blade -m ""{{ 'name': 'John Doe' }}""
");
        }
        static string GetEngineAssemblyPath(string arg)
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
        static bool CreateRunner(BladeRunnerOptions options, out BladeRunner runner)
        {
            var result = false;

            runner = null;

            do
            {
                // STEP 2. Validate Egnine and extract its runner

                if (IsSomeString(options.EngineLibraryPath))
                {
                    var assembly = logger.Try($"Loading Engine assembly '{options.EngineLibraryPath}' ...", options.Debug, () => Assembly.LoadFrom(options.EngineLibraryPath));

                    if (assembly != null)
                    {
                        var runnerType = logger.Try($"Finding runner ...", options.Debug, () => assembly.GetTypes().FirstOrDefault(t => t.DescendsFrom(typeof(BladeRunner))));

                        if (runnerType == null)
                        {
                            logger.Log($"Could not find a runner in '{options.EngineName}' engine assembly that is derived from BladeRunner base class.");
                            break;
                        }
                        else
                        {
                            runner = logger.Try($"Instantiating runner ...", options.Debug, () => (BladeRunner)Activator.CreateInstance(runnerType, logger));

                            if (runner == null)
                            {
                                logger.Abort($"Instantiating '{options.EngineName}' runner failed", !options.Debug);
                                break;
                            }
                        }
                    }
                    else
                    {
                        logger.Abort($"Loading '{options.EngineName}' engine assembly failed", !options.Debug);
                    }
                }
                else
                {
                    logger.Log("No engine specified or specified engine is invalid.");
                    break;
                }

                options.DefaultOutputExtensions = runner.EngineConfig.FileExtension;

                result = true;
            } while (false);

            return result;
        }
        static bool IsArgValue(string arg)
        {
            return !arg.StartsWith("-") && string.Compare(arg, "runner", true) != 0 && string.Compare(arg, "/?", true) != 0 && !IsSomeString(arg, true);
        }
        static BladeRunnerOptions GetOptions(string[] args)
        {
            var result = new BladeRunnerOptions();

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
                    result.LogRunnerOutput = true;
                    continue;
                }

                if (arg == "-i")
                {
                    if (i < args.Length - 1)
                    {
                        if (args[i + 1].IndexOf('<') >= 0)
                        {
                            result.Input = args[i + 1];
                        }
                        else
                        {
                            result.InputFile = args[i + 1];
                        }
                    }

                    continue;
                }

                if (arg == "-o")
                {
                    if (i < args.Length - 1 && IsArgValue(args[i + 1]))
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
                    if (i < args.Length - 1 && IsArgValue(args[i + 1]))
                    {
                        result.RunnerOutputFile = args[i + 1];
                        result.RunnerOutputMode = OutputMode.User;
                    }
                    else
                    {
                        result.RunnerOutputMode = OutputMode.Auto;
                    }

                    continue;
                }

                if (arg == "-ch")
                {
                    if (i < args.Length - 1 && IsArgValue(args[i + 1]))
                    {
                        result.CacheDir = args[i + 1];
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
                        var path = GetEngineAssemblyPath(args[i + 1]);

                        if (IsSomeString(path))
                        {
                            result.EngineLibraryPath = path;
                            result.EngineName = args[i + 1].ToLower();
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

                if (CreateRunner(options, out runner))
                {
                    if (options.Debug)
                    {
                        logger.Log(Environment.NewLine + "Given options:");
                        logger.Debug(Environment.NewLine + JsonConvert.SerializeObject(options, Formatting.Indented) + Environment.NewLine);
                    }

                    runner.Run(options);
                }
            }
        }
        static void Main(string[] args)
        {
            Start(args);
        }
    }
}
